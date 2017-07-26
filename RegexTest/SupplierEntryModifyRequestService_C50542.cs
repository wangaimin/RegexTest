using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Transactions;
using YZ.JC.API.DataAccess;
using YZ.JC.Entity;
using YZ.JC.Entity.API;
using YZ.JC.Entity.Common;
using YZ.JC.Entity.ViewModel;
using YZ.JC.SupplierMgt.DataAccess;
using YZ.JC.SupplierMgtService.DataAccess;
using YZ.JsonRpc.Client;
using YZ.SupplierMgtService.Service;
using YZ.Utility;
using YZ.Utility.DataAccess;
using YZ.Utility.EntityBasic;

namespace YZ.JC.SupplierMgt.Service
{
    public class SupplierEntryModifyRequestService
    {
        /// <summary>
        /// 分页查询
        /// </summary>
        public QueryResult<SupplierEntryModifyRequestQueryResult> Query(SupplierEntryModifyRequestQueryFilter filter)
        {
            if (filter.RequestDateTo.HasValue)
            {
                filter.RequestDateTo = filter.RequestDateTo.Value.AddDays(1);
            }
            return SupplierEntryModifyRequestDA.Query(filter);
        }


        /// <summary>
        /// 加载变更品类最近一次变更的状态
        /// </summary>
        /// <param name="supplierSysNo"></param>
        /// <param name="organizationSysNo"></param>
        /// <param name="joinSupplierCategorySysNo"></param>
        /// <returns></returns>
        public SupplierModifyRequestStatusInfo LoadCategoryLatestStatusInfo(int supplierSysNo, int organizationSysNo, int joinSupplierCategorySysNo)
        {
            SupplierEntryModifyRequestType requestType = SupplierEntryModifyRequestType.Category;
            return SupplierEntryModifyRequestDA.LoadCategoryLatestStatusInfo(requestType, supplierSysNo, organizationSysNo, joinSupplierCategorySysNo);
        }

        /// <summary>
        /// 加载变更品类最近一次变更的状态
        /// </summary>
        /// <param name="supplierSysNo"></param>
        /// <returns></returns>
        public SupplierModifyRequestStatusInfo LoadFormLatestStatusInfo(int supplierSysNo)
        {
            SupplierEntryModifyRequestType requestType = SupplierEntryModifyRequestType.FormInfo;
            return SupplierEntryModifyRequestDA.LoadFormLatestStatusInfo(requestType, supplierSysNo);
        }

        /// <summary>
        /// 根据系统编号加载变更品类详情
        /// </summary>
        /// <param name="requestSysNo"></param>
        public SupplierEntryModifyCategoryDetail LoadModifyCategoryDetail(int requestSysNo)
        {
            var requestInfo = SupplierEntryModifyRequestDA.LoadCategoryModifyRequest(requestSysNo);
            if (requestInfo == null)
            {
                throw new BusinessException("找不到变更请求信息#" + requestSysNo.ToString());
            }
            CheckIsMyData(requestInfo.SysNo, requestInfo.OrganizationCode);
            SupplierEntryModifyCategoryDetail result = new SupplierEntryModifyCategoryDetail();
            result.OrganizationName = requestInfo.OrganizationName;
            result.JoinSupplierCategoryName = requestInfo.JoinSupplierCategoryName;
            result.SupplierSysNo = requestInfo.SupplierSysNo;
            result.SupplierName = requestInfo.SupplierName;
            result.AuthStatus = requestInfo.AuthStatus;

            //加载申请的商品分类
            var productCategoryList = SupplierEntryModifyRequestDA.LoadProductCategory(requestSysNo);
            result.ProductCategoryNames = productCategoryList.OrderBy(item => item.ProductCategorySysNo).Select(item => item.ProductCategoryName);

            //加载申请的供应商分类
            var supplierCategoryList = SupplierEntryModifyRequestDA.LoadSupplierCategory(requestSysNo);
            result.SupplierCategoryNames = supplierCategoryList.OrderBy(item => item.SupplierCategorySysNo).Select(item => item.CategoryName);

            //加载旧的商品分类
            var oldProductCategoryList = LoadSupplierOldProductCategory(requestInfo.SupplierSysNo, requestInfo.OrganizationSysNo, requestInfo.JoinSupplierCategorySysNo);
            result.OldProductCategoryNames = oldProductCategoryList.OrderBy(item => item.SysNo).Select(item => (string)item.CategoryName);

            //加载旧的供应商分类
            var oldSupplierCategoryList = LoadSupplierOldSupplierCategory(requestInfo.SupplierSysNo, requestInfo.OrganizationSysNo, requestInfo.JoinSupplierCategorySysNo);
            result.OldSupplierCategoryNames = oldSupplierCategoryList.OrderBy(item => item.SupplierCategorySysNo).Select(item => (string)item.CategoryName);

            return result;
        }

        /// <summary>
        /// 根据系统编号加载变更准入资料详情
        /// </summary>
        /// <param name="requestSysNo"></param>
        public SupplierEntryModifyRequestFormInfo LoadModifyFormDetail(int requestSysNo)
        {
            var result = LoadFormInfo(requestSysNo);
            CheckIsMyData(result.SysNo, result.OrganizationCode);
            var supplierBasicInfo = SupplierBasicInfoDA.LoadBasicInfo(result.SupplierSysNo);
            if (supplierBasicInfo == null)
            {
                throw new BusinessException("找不到供应商#" + result.SupplierSysNo.ToString());
            }
            //三证合一时，清空相关的字段
            if (!string.IsNullOrWhiteSpace(supplierBasicInfo.UnifiedSocialCode))
            {
                result.PassportValidTo = null;
                result.CertificateCardValidTo = null;
                supplierBasicInfo.RegisterNumber = string.Empty;
                supplierBasicInfo.CertificateCardCode = string.Empty;
                supplierBasicInfo.TaxLicense = string.Empty;
            }
            result.BasicInfo = supplierBasicInfo;

            //处理企业资质信用及相关附件
            var creditInfos = result.CreditInfos;
            var fileAttachments = result.FileAttachments;
            //为了性能考虑，不再返回这两个原始的数据，下面会进行详细的加工处理
            result.CreditInfos = null;
            result.FileAttachments = null;

            //三证
            result.UIEnterpriseFileGroupList = new List<SupplierEntryFileGroup>();
            //营业执照
            SupplierEntryFileGroup passportFileGroup = new SupplierEntryFileGroup();
            passportFileGroup.Name = SupplierAttachmentType.BusinessLicence.GetDescription();
            passportFileGroup.PassportDate = supplierBasicInfo.UIPassportDate;
            passportFileGroup.FileList = fileAttachments.Where(item => item.AttachmentType == SupplierAttachmentType.BusinessLicence);
            result.UIEnterpriseFileGroupList.Add(passportFileGroup);
            //三证合一时不显示组织机构代码证和税务登记证
            if (!string.IsNullOrWhiteSpace(supplierBasicInfo.UnifiedSocialCode))
            {
                //组织机构代码证
                SupplierEntryFileGroup organizationLicenceFileGroup = new SupplierEntryFileGroup();
                organizationLicenceFileGroup.Name = SupplierAttachmentType.OrganizationLicence.GetDescription();
                organizationLicenceFileGroup.PassportDate = supplierBasicInfo.UICertificateCardDate;
                organizationLicenceFileGroup.FileList = fileAttachments.Where(item => item.AttachmentType == SupplierAttachmentType.OrganizationLicence);
                result.UIEnterpriseFileGroupList.Add(organizationLicenceFileGroup);

                //税务登记证
                SupplierEntryFileGroup taxLicenceFileGroup = new SupplierEntryFileGroup();
                taxLicenceFileGroup.Name = SupplierAttachmentType.TaxLicence.GetDescription();
                taxLicenceFileGroup.PassportDate = supplierBasicInfo.UITaxLicenseDate;
                taxLicenceFileGroup.FileList = fileAttachments.Where(item => item.AttachmentType == SupplierAttachmentType.TaxLicence);
                result.UIEnterpriseFileGroupList.Add(taxLicenceFileGroup);
            }
            //企业授权书
            SupplierEntryFileGroup enterpriseWarrantFileGroup = new SupplierEntryFileGroup();
            enterpriseWarrantFileGroup.Name = SupplierAttachmentType.Warrant.GetDescription();
            enterpriseWarrantFileGroup.FileList = fileAttachments.Where(item => item.AttachmentType == SupplierAttachmentType.Warrant);
            result.UIEnterpriseFileGroupList.Add(enterpriseWarrantFileGroup);

            //公司名变更证明
            var changeNameWarrantFileList = fileAttachments.Where(item => item.AttachmentType == SupplierAttachmentType.ChangeNameWarrant);
            if (changeNameWarrantFileList.Count() > 0)
            {
                SupplierEntryFileGroup changeNameWarrantFileGroup = new SupplierEntryFileGroup();
                changeNameWarrantFileGroup.Name = SupplierAttachmentType.ChangeNameWarrant.GetDescription();
                changeNameWarrantFileGroup.FileList = changeNameWarrantFileList;
                result.UIEnterpriseFileGroupList.Add(changeNameWarrantFileGroup);
            }

            //法人手机号修改授权书
            var changePhoneWarrantFileList = fileAttachments.Where(item => item.AttachmentType == SupplierAttachmentType.ChangePhoneWarrant);
            if (changeNameWarrantFileList.Count() > 0)
            {
                SupplierEntryFileGroup changePhoneWarrantFileGroup = new SupplierEntryFileGroup();
                changePhoneWarrantFileGroup.Name = SupplierAttachmentType.ChangePhoneWarrant.GetDescription();
                changePhoneWarrantFileGroup.FileList = changePhoneWarrantFileList;
                result.UIEnterpriseFileGroupList.Add(changePhoneWarrantFileGroup);
            }

            //企业资质信用
            result.UICreditFileGroupList = new List<SupplierEntryFileGroup>();
            foreach (var ci in creditInfos)
            {
                SupplierEntryFileGroup creditFileGroup = new SupplierEntryFileGroup();
                creditFileGroup.Name = ci.CreditType.GetDescription();

                if (ci.CreditType != SupplierCreditType.SafetyLicense &&
                            ci.CreditType != SupplierCreditType.Certification)
                {
                    //设置企业资质信用等级
                    creditFileGroup.CreditLevel = ci.CreditLevel;
                    creditFileGroup.FileList = fileAttachments.Where(item => item.AttachmentType == (SupplierAttachmentType)ci.CreditType);
                }
                result.UICreditFileGroupList.Add(creditFileGroup);
            }
            //资质证书（正、副本）
            var certificationFileList = fileAttachments.Where(item => item.AttachmentType == SupplierAttachmentType.Certification);
            SupplierEntryFileGroup certificationFileGroup = new SupplierEntryFileGroup();
            certificationFileGroup.Name = SupplierAttachmentType.Certification.GetDescription();
            certificationFileGroup.FileList = certificationFileList;
            result.UICreditFileGroupList.Add(certificationFileGroup);
            //安全施工许可证（正、副本）
            var safetyConstructionFileList = fileAttachments.Where(item => item.AttachmentType == SupplierAttachmentType.SafetyConstruction);
            SupplierEntryFileGroup safetyConstructionFileGroup = new SupplierEntryFileGroup();
            safetyConstructionFileGroup.Name = SupplierAttachmentType.SafetyConstruction.GetDescription();
            safetyConstructionFileGroup.FileList = safetyConstructionFileList;
            result.UICreditFileGroupList.Add(safetyConstructionFileGroup);

            //其他必须附件
            result.UIOtherRequiredFileGroupList = new List<SupplierEntryFileGroup>();
            var otherRequiredAttachmentGroups = fileAttachments.Where(item => item.AttachmentType >= SupplierAttachmentType.LegalPersonID
            && item.AttachmentType < SupplierAttachmentType.AgentCertificate).GroupBy(item => item.AttachmentType);
            foreach (var attachmentGroup in otherRequiredAttachmentGroups)
            {
                SupplierEntryFileGroup fileGroup = new SupplierEntryFileGroup();
                fileGroup.Name = attachmentGroup.Key.GetDescription();
                fileGroup.FileList = attachmentGroup.ToList();
                result.UIOtherRequiredFileGroupList.Add(fileGroup);
            }

            //其他若有附件
            result.UIOtherOptionalFileGroupList = new List<SupplierEntryFileGroup>();
            var otherOptionalAttachmentGroups = fileAttachments.Where(item => item.AttachmentType >= SupplierAttachmentType.AgentCertificate
            && item.AttachmentType < SupplierAttachmentType.Investigation).GroupBy(item => item.AttachmentType);
            foreach (var attachmentGroup in otherOptionalAttachmentGroups)
            {
                SupplierEntryFileGroup fileGroup = new SupplierEntryFileGroup();
                fileGroup.Name = attachmentGroup.Key.GetDescription();
                fileGroup.FileList = attachmentGroup.ToList();
                result.UIOtherOptionalFileGroupList.Add(fileGroup);
            }

            return result;
        }

        /// <summary>
        /// 插入变更品类
        /// </summary>
        /// <param name="request"></param>
        public void RequestModifyCategory(SupplierEntryModifyRequestCategory data)
        {
            data.RequestType = SupplierEntryModifyRequestType.Category;
            CheckRequestCategory(data.SupplierSysNo, data.JoinSupplierCategorySysNo, data.RequestType);

            TransactionOptions tsOptions = new TransactionOptions();
            //设置隔离级别
            tsOptions.IsolationLevel = IsolationLevel.ReadCommitted;
            // 设置超时间隔为2分钟，默认为60秒
            tsOptions.Timeout = new TimeSpan(0, 2, 0);
            using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required, tsOptions))
            {
                SupplierEntryModifyRequestDA.InsertRequestCategory(data);

                SupplierEntryModifyRequestDA.BatchInsertProductCategory(data.SysNo, data.SupplierSysNo, data.ProductCategories, data.InUserSysNo, data.InUserName);

                SupplierEntryModifyRequestDA.BatchInsertSupplierCategory(data.SysNo, data.SupplierSysNo, data.SupplierCategories, data.InUserSysNo, data.InUserName);

                ts.Complete();
            }
        }

        /// <summary>
        /// 插入变更
        /// </summary>
        /// <param name="request"></param>
        public void RequestModifyFormInfo(SupplierEntryModifyRequestFormInfo data)
        {
            data.RequestType = SupplierEntryModifyRequestType.FormInfo;
            CheckRequestFormInfo(data.SupplierSysNo, data.RequestType);

            //查询首次准入该供应商的组织机构和分类
            var firstPassedEntryStatusInfo = Supplier_ManagementDA.LoadFirstPassedEntryStatusInfo(data.SupplierSysNo);
            if (firstPassedEntryStatusInfo == null)
            {
                throw new BusinessException("您还没有进行分供商认证或认证未通过，暂无法申请变更。");
            }
            //提交给首次准入通过的分类
            data.JoinSupplierCategorySysNo = firstPassedEntryStatusInfo.JoinSupplierCategorySysNo;
            data.JoinSupplierCategoryCode = firstPassedEntryStatusInfo.JoinSupplierCategoryCode;

            //联系人手机号已做了短信验证码验证，因此可以自动标志为验证通过
            List<UnverifiedTelPhone> autoVerifiedCellPhoneList = new List<UnverifiedTelPhone>();
            autoVerifiedCellPhoneList.Add(new UnverifiedTelPhone
            {
                TelPhone = data.ContactPhone,
                EnableSubscribe = true,
                UserSysNo = data.InUserSysNo
            });

            TransactionOptions tsOptions = new TransactionOptions();
            //设置隔离级别
            tsOptions.IsolationLevel = IsolationLevel.ReadCommitted;
            // 设置超时间隔为2分钟，默认为60秒
            tsOptions.Timeout = new TimeSpan(0, 2, 0);
            using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required, tsOptions))
            {
                //插入基本信息
                SupplierEntryModifyRequestDA.InsertRequestFormInfo(data);

                //插入主要经营区域
                SupplierEntryModifyRequestDA.BatchInsertMainBusinessArea(data.SysNo, data.SupplierSysNo, data.MainBusinessAreaIDs, data.InUserSysNo, data.InUserName);

                //插入合作信息
                SupplierEntryModifyRequestDA.BatchInsertCoperationInfo(data.SysNo, data.SupplierSysNo, data.CoperationInfos, data.InUserSysNo, data.InUserName);

                //插入主要管理人员
                SupplierEntryModifyRequestDA.BatchInsertManagerInfo(data.SysNo, data.SupplierSysNo, data.ManagerInfos, data.InUserSysNo, data.InUserName);

                //插入资质及附件
                SupplierEntryModifyRequestDA.BatchInsertCreditInfo(data.SysNo, data.SupplierSysNo, data.CreditInfos, data.InUserSysNo, data.InUserName);
                SupplierEntryModifyRequestDA.BatchInsertFileAttachment(data.SysNo, data.SupplierSysNo, data.FileAttachments, data.InUserSysNo, data.InUserName);

                //自动将联系人手机号标志为验证通过
                Rpc.Call<bool>("CommonService.EmailAndSMSService.SaveValidUserPhone", autoVerifiedCellPhoneList);

                ts.Complete();
            }
        }

        /// <summary>
        /// 接受变更申请
        /// </summary>
        /// <param name="request"></param>
        public void AcceptModifyCategory(int requestSysNo, string authMemo, int inUserSysNo, string inUserName)
        {
            var requestBaseInfo = SupplierEntryModifyRequestDA.LoadFormInfo(requestSysNo);
            if (requestBaseInfo == null)
            {
                throw new BusinessException("找不到相关申请信息。编号为#" + requestSysNo);
            }
            if (requestBaseInfo.AuthStatus != SupplierModifyAuthStatus.Auditing)
            {
                throw new BusinessException(string.Format("申请信息当前状态是[{0}]，不能执行此操作", requestBaseInfo.AuthStatus.GetDescription()));
            }
            CheckIsMyData(requestBaseInfo.SysNo, requestBaseInfo.OrganizationCode);
            if (string.IsNullOrWhiteSpace(requestBaseInfo.OrganizationCode) || requestBaseInfo.OrganizationCode.Trim().Length <= 8)
            {
                throw new BusinessException("变更所属组织机构编码无效。");
            }
            //根据加入的分类计算出一级品类系统编号
            var joinSupplierCategory = SupplierCategoryDA.LoadSupplierCategory(requestBaseInfo.JoinSupplierCategorySysNo);
            if (joinSupplierCategory == null)
            {
                throw new BusinessException("找不到准入分类信息。编号为#" + requestBaseInfo.JoinSupplierCategorySysNo);
            }
            //查询供应商变更前的信息并序列化为json
            var oldSupplierCategory = LoadSupplierOldSupplierCategory(requestBaseInfo.SupplierSysNo, requestBaseInfo.OrganizationSysNo, requestBaseInfo.JoinSupplierCategorySysNo);
            var oldProductCategory = LoadSupplierOldProductCategory(requestBaseInfo.SupplierSysNo, requestBaseInfo.OrganizationSysNo, requestBaseInfo.JoinSupplierCategorySysNo);
            string supplierOldInfoJson = SerializeHelper.JsonSerializeFixed(new
            {
                OldSupplierCategory = oldSupplierCategory,
                OldProductCategory = oldProductCategory
            });
            List<int> newSupplierCategory = SupplierEntryModifyRequestDA.LoadSupplierCategory(requestSysNo).Select(item => item.SupplierCategorySysNo).ToList();
            List<int> newProductCategory = SupplierEntryModifyRequestDA.LoadProductCategory(requestSysNo).Select(item => item.ProductCategorySysNo).ToList();

            TransactionOptions tsOptions = new TransactionOptions();
            tsOptions.IsolationLevel = IsolationLevel.ReadCommitted;
            tsOptions.Timeout = new TimeSpan(0, 2, 0);
            using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required, tsOptions))
            {
                //插入变更前的供应商信息
                SupplierEntryModifyRequest_SupplierOldInfoDA.Insert(requestSysNo, requestBaseInfo.SupplierSysNo, supplierOldInfoJson, inUserSysNo, inUserName);

                //更新商品分类,品类以及供应商分类
                string stockOrganizationCode = requestBaseInfo.OrganizationCode.Trim().Substring(0, 8) + "%";

                Supplier_ManagementDA.BatchUpdateStockSupplierCategory(requestBaseInfo.SupplierSysNo, stockOrganizationCode,
                    requestBaseInfo.JoinSupplierCategorySysNo, requestBaseInfo.JoinSupplierCategoryCode, joinSupplierCategory.SystemCategorySysNo,
                    newProductCategory, newSupplierCategory,
                    inUserSysNo, inUserName);

                //更新审核状态信息等
                SupplierEntryModifyRequestDA.UpdateAuthStatus(requestSysNo, SupplierModifyAuthStatus.AuditPass, authMemo, inUserSysNo, inUserName);

                ts.Complete();
            }

            var supplierInfo = SupplierDA.LoadSupplier(requestBaseInfo.SupplierSysNo);
            if (!string.IsNullOrWhiteSpace(supplierInfo.ContactPhone))
            {
                var sendResult = Rpc.Call<bool>(
                    JicaiRpcServiceMethods.CommonService_EmailAndSMSService_SendSMS,
                    supplierInfo.ContactPhone,
                    SMSType.SupplierEntryModifyCategoryAuditPass.ToString(),
                    SMSType.SupplierEntryModifyCategoryAuditPass,
                    inUserSysNo,
                    inUserName,
                    "",
                    new object[] { }
                );
            }
        }

        /// <summary>
        /// 接受变更申请
        /// </summary>
        /// <param name="request"></param>
        public void AcceptModifyFormInfo(int requestSysNo, string authMemo, int inUserSysNo, string inUserName)
        {
            var formInfo = this.LoadFormInfo(requestSysNo);
            CheckIsMyData(formInfo.SysNo, formInfo.OrganizationCode);
            //查询供应商变更前的信息并序列化为json
            object oldSupplierInfo = LoadSupplierOldInfo(formInfo.SupplierSysNo);
            string supplierOldInfoJson = SerializeHelper.JsonSerializeFixed(oldSupplierInfo);

            TransactionOptions tsOptions = new TransactionOptions();
            tsOptions.IsolationLevel = IsolationLevel.ReadCommitted;
            tsOptions.Timeout = new TimeSpan(0, 2, 0);
            using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required, tsOptions))
            {
                //插入变更前的供应商信息
                SupplierEntryModifyRequest_SupplierOldInfoDA.Insert(requestSysNo, formInfo.SupplierSysNo, supplierOldInfoJson, inUserSysNo, inUserName);

                //更新供应商的帐号上的手机号为联系人的手机号
                if (!string.IsNullOrWhiteSpace(formInfo.ContactPhone))
                {
                    Rpc.Call<dynamic>(
                    JicaiRpcServiceMethods.AuthService_SystemUser_UpdateSupplierUserCellPhone, formInfo.SupplierSysNo, formInfo.ContactPhone, inUserSysNo, inUserName);
                }
                //更新供应商信息
                Supplier_ManagementDA.UpdateEntryInfo(formInfo, inUserSysNo, inUserName);

                //插入主要经营区域
                Supplier_MainBusinessAreaDA.BatchUpdateMainBusinessArea(formInfo.SupplierSysNo, inUserSysNo, inUserName, formInfo.MainBusinessAreaIDs);

                //更新合作信息
                APISupplierDA.BatchInsertCoperationInfo(formInfo.CoperationInfos, formInfo.SupplierSysNo, inUserSysNo, inUserName);

                //更新主要管理人员
                APISupplierDA.BatchInsertManagerInfo(formInfo.ManagerInfos, formInfo.SupplierSysNo, inUserSysNo, inUserName);

                //更新资质及附件
                APISupplierDA.BatchInsertCreditInfo(formInfo.CreditInfos, formInfo.SupplierSysNo, inUserSysNo, inUserName);
                SupplierAttachmentType smallAttachmentType = SupplierAttachmentType.BanckCredit;
                SupplierAttachmentType largeAttachmentType = SupplierAttachmentType.Investigation;
                var onlyCreditFileAttachments = formInfo.FileAttachments.Where(item => item.AttachmentType >= smallAttachmentType && item.AttachmentType < largeAttachmentType).ToList();
                Supplier_ManagementDA.BatchInsertCreditFileAttachment(onlyCreditFileAttachments, formInfo.SupplierSysNo, inUserSysNo, inUserName, smallAttachmentType, largeAttachmentType);

                //更新审核状态信息等
                SupplierEntryModifyRequestDA.UpdateAuthStatus(requestSysNo, SupplierModifyAuthStatus.AuditPass, authMemo, inUserSysNo, inUserName);

                ts.Complete();
            }

            var supplierInfo = SupplierDA.LoadSupplier(formInfo.SupplierSysNo);
            if (!string.IsNullOrWhiteSpace(supplierInfo.ContactPhone))
            {
                var sendResult = Rpc.Call<bool>(
                    JicaiRpcServiceMethods.CommonService_EmailAndSMSService_SendSMS,
                    supplierInfo.ContactPhone,
                    SMSType.SupplierEntryModifyFormAuditPass.ToString(),
                    SMSType.SupplierEntryModifyFormAuditPass,
                    inUserSysNo,
                    inUserName,
                    "",
                    new object[] { }
                );
            }
        }

        /// <summary>
        /// 拒绝变更申请
        /// </summary>
        /// <param name="request"></param>
        public void RefuseModify(int requestSysNo, string authMemo, int inUserSysNo, string inUserName)
        {
            if (string.IsNullOrWhiteSpace(authMemo))
            {
                throw new BusinessException("请填写审核备注。");
            }
            authMemo = authMemo.Trim();
            if (authMemo.Length > 200)
            {
                throw new BusinessException("审核备注不能超过200字。");
            }

            var requestBaseInfo = SupplierEntryModifyRequestDA.LoadFormInfo(requestSysNo);
            if (requestBaseInfo == null)
            {
                throw new BusinessException("找不到相关申请信息。编号为#" + requestSysNo);
            }
            if (requestBaseInfo.AuthStatus != SupplierModifyAuthStatus.Auditing)
            {
                throw new BusinessException(string.Format("申请信息当前状态是[{0}]，不能执行此操作", requestBaseInfo.AuthStatus.GetDescription()));
            }

            //更新审核状态信息等
            SupplierEntryModifyRequestDA.UpdateAuthStatus(requestSysNo, SupplierModifyAuthStatus.AuditNotPass, authMemo, inUserSysNo, inUserName);

            var reqeustInfo = SupplierEntryModifyRequestDA.LoadFormInfo(requestSysNo);
            var supplierInfo = SupplierDA.LoadSupplier(reqeustInfo.SupplierSysNo);
            SMSType smsType = reqeustInfo.RequestType == SupplierEntryModifyRequestType.Category ?
                SMSType.SupplierEntryModifyCategoryAuditNoPass : SMSType.SupplierEntryModifyFormAuditNoPass;
            if (!string.IsNullOrWhiteSpace(supplierInfo.ContactPhone))
            {
                var sendResult = Rpc.Call<bool>(
                    JicaiRpcServiceMethods.CommonService_EmailAndSMSService_SendSMS,
                    supplierInfo.ContactPhone,
                    smsType.ToString(),
                    smsType,
                    inUserSysNo,
                    inUserName,
                    "",
                    new object[] { authMemo }
                );
            }
        }

        public void CancelModifyFormInfo(int supplierSysNo, int operateUserSysNo, string operateUserName)
        {
            SupplierEntryModifyRequestDA.CancelModifyRequest(supplierSysNo, SupplierEntryModifyRequestType.FormInfo, operateUserSysNo, operateUserName);
        }

        public void CancelModifyCategory(int supplierSysNo, int supplierEntryModifyRequestSysNo, int operateUserSysNo, string operateUserName)
        {
            //check status
            var requestInfo = SupplierEntryModifyRequestDA.LoadCategoryModifyRequest(supplierEntryModifyRequestSysNo);
            if (requestInfo == null)
            {
                throw new BusinessException(string.Format("申请编号#{0}不存在", supplierEntryModifyRequestSysNo));
            }
            if (requestInfo.SupplierSysNo != supplierSysNo)
            {
                throw new BusinessException(string.Format("供应商编号#{0}不匹配，请检查数据来源", supplierSysNo));
            }
            if (requestInfo.AuthStatus != SupplierModifyAuthStatus.Auditing)
            {
                throw new BusinessException(string.Format("当前状态是[{0}]，不能执行[撤销]", requestInfo.AuthStatus.GetDescription()));
            }

            SupplierEntryModifyRequestDA.CancelModifyRequestBySysNo(supplierSysNo, supplierEntryModifyRequestSysNo, operateUserSysNo, operateUserName);
        }

        #region Private Methods
        private List<dynamic> LoadSupplierOldSupplierCategory(int supplierSysNo, int organizationSysNo, int joinSupplierCategorySysNo)
        {
            QF_Supplier_MyLibrary filter = new QF_Supplier_MyLibrary();
            filter.SupplierSysNo = supplierSysNo;
            filter.OrganizationSysNo = organizationSysNo;
            filter.JoinSupplierCategorySysNo = joinSupplierCategorySysNo;

            Supplier_SupplierCategoryService supplierCategoryService = new Supplier_SupplierCategoryService();
            var supplierCategoryList = supplierCategoryService.GetSupplierCategoryListBySupplierSysNo(filter).OrderBy(item => item.SupplierCategorySysNo);

            List<dynamic> result = new List<dynamic>();
            foreach (var item in supplierCategoryList)
            {
                dynamic data = new ExpandoObject();
                data.SupplierCategorySysNo = item.SupplierCategorySysNo;
                data.SupplierCategoryCode = item.SupplierCategoryCode;
                data.CategoryName = item.CategoryName;
                result.Add(data);
            }

            return result;
        }

        private List<dynamic> LoadSupplierOldProductCategory(int supplierSysNo, int organizationSysNo, int joinSupplierCategorySysNo)
        {
            QF_Supplier_MyLibrary filter = new QF_Supplier_MyLibrary();
            filter.SupplierSysNo = supplierSysNo;
            filter.OrganizationSysNo = organizationSysNo;
            filter.SupplierCategorySysNo = joinSupplierCategorySysNo;

            Supplier_SupplierCategoryService supplierCategoryService = new Supplier_SupplierCategoryService();
            var productCategoryList = supplierCategoryService.GetProductCategoryBySupplierSysNo(filter).OrderBy(item => item.SysNo);

            List<dynamic> result = new List<dynamic>();
            foreach (var item in productCategoryList)
            {
                dynamic data = new ExpandoObject();
                data.SysNo = item.SysNo;
                data.CategoryCode = item.CategoryCode;
                data.CategoryName = item.CategoryName;
                result.Add(data);
            }

            return result;
        }

        private object LoadSupplierOldInfo(int supplierSysNo)
        {
            var supplier = APISupplierDA.LoadSupplierByOldSysNo(supplierSysNo);
            var allCoperationInfo = APISupplierDA.LoadAllCoperationInfo(supplier.SysNo);
            var allManagerInfo = APISupplierDA.LoadAllManagerInfo(supplier.SysNo);
            var allFileAttachment = APISupplierDA.LoadAllFileAttachment(supplier.SysNo);
            var allCreditInfo = APISupplierDA.LoadAllCreditInfo(supplier.SysNo);

            return new
            {
                Supplier = supplier,
                CoperationInfo = allCoperationInfo,
                ManagerInfo = allManagerInfo,
                FileAttachment = allFileAttachment,
                CreditInfo = allCreditInfo
            };
        }

        private SupplierEntryModifyRequestFormInfo LoadFormInfo(int requestSysNo)
        {
            var result = SupplierEntryModifyRequestDA.LoadFormInfo(requestSysNo);
            if (result == null)
            {
                throw new BusinessException("找不到变更请求信息#" + requestSysNo.ToString());
            }
            var mainBusinessAreaList = SupplierEntryModifyRequestDA.LoadMainBusinessArea(requestSysNo);
            result.MainBusinessAreaIDs = mainBusinessAreaList.OrderBy(item => item.SysNo).Select(item => item.SysNo).ToList();
            result.MainBusinessAreaNames = mainBusinessAreaList.OrderBy(item => item.SysNo).Select(item => item.AreaName).ToList();
            result.CoperationInfos = SupplierEntryModifyRequestDA.LoadCoperationInfo(requestSysNo);
            result.ManagerInfos = SupplierEntryModifyRequestDA.LoadManagerInfo(requestSysNo);
            //企业资质信用及相关附件
            result.CreditInfos = SupplierEntryModifyRequestDA.LoadCreditInfo(requestSysNo);
            result.FileAttachments = SupplierEntryModifyRequestDA.LoadFileAttachment(requestSysNo);

            return result;
        }

        private void CheckRequestFormInfo(int supplierSysNo, SupplierEntryModifyRequestType requestType)
        {
            SupplierEntryModifyRequestQueryFilter filter = new SupplierEntryModifyRequestQueryFilter();
            filter.PageSize = 1;
            filter.AuthStatus = SupplierModifyAuthStatus.Auditing;
            filter.RequestType = requestType;
            filter.SupplierSysNo = supplierSysNo;
            filter.PassDataPermissionAuth = true;

            var queryResult = SupplierEntryModifyRequestDA.Query(filter);

            if (queryResult.recordsTotal > 0)
            {
                throw new BusinessException("当前已存在审核中的变更，请勿重复申请。");
            }
        }

        private void CheckRequestCategory(int supplierSysNo, int joinSupplierCategorySysNo, SupplierEntryModifyRequestType requestType)
        {
            SupplierEntryModifyRequestQueryFilter filter = new SupplierEntryModifyRequestQueryFilter();
            filter.PageSize = 1;
            filter.AuthStatus = SupplierModifyAuthStatus.Auditing;
            filter.RequestType = requestType;
            filter.SupplierSysNo = supplierSysNo;
            filter.JoinSupplierCategorySysNo = joinSupplierCategorySysNo;
            filter.PassDataPermissionAuth = true;

            var queryResult = SupplierEntryModifyRequestDA.Query(filter);

            if (queryResult.recordsTotal > 0)
            {
                throw new BusinessException("当前已存在审核中的变更，请勿重复申请。");
            }
        }

        private void CheckIsMyData(int entryRequestSysNo, string organizationCode)
        {
            if (!string.IsNullOrWhiteSpace(organizationCode))
            {
                organizationCode = organizationCode.Trim();
            }
            bool isMyData = DataPermissionHelper.IsMyData(organizationCode);
            if (!isMyData)
            {
                throw new BusinessException(string.Format("单据#{0}不是您的数据，请勿越权访问。", entryRequestSysNo));
            }
        }

        #endregion
    }
}
