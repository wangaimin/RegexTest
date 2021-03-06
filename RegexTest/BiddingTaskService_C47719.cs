using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using YZ.Utility;
using YZ.JC.InquiryMgtService.DataAccess;
using YZ.JC.Entity;
using YZ.Utility.EntityBasic;
using YZ.JC.Entity.ViewModel;
using YZ.JC.InquiryMgt.Service;

namespace YZ.JC.InquiryMgtService.Service
{
    public class BiddingTaskService
    {

        /// <summary>
        /// 创建BiddingTask信息
        /// </summary>
        public int InsertBiddingTask(BiddingTask entity)
        {
            CheckBiddingTask(entity, true);
            return BiddingTaskDA.InsertBiddingTask(entity);
        }



        /// <summary>
        /// 更新BiddingTask信息
        /// </summary>
        public void UpdateBiddingTask(BiddingTask entity)
        {
            CheckBiddingTask(entity, false);
            BiddingTaskDA.UpdateBiddingTask(entity);
        }



        /// <summary>
        /// 删除BiddingTask信息
        /// </summary>
        public void DeleteBiddingTask(int sysNo)
        {
            BiddingTaskDA.DeleteBiddingTask(sysNo);
        }



        /// <summary>
        /// 分页查询BiddingTask信息
        /// </summary>
        public QueryResult<QR_BiddingTask> QueryBiddingTaskList(QF_BiddingTask filter)
        {
            return BiddingTaskDA.QueryBiddingTaskList(filter);
        }



        /// <summary>
        /// 加载BiddingTask信息
        /// </summary>
        public BiddingTask LoadBiddingTask(int sysNo)
        {
            return BiddingTaskDA.LoadBiddingTask(sysNo);
        }
        /// <summary>
        /// 根据招标编号获取最新一轮的调价任务
        /// </summary>
        /// <param name="tenderSysNo"></param>
        /// <returns></returns>
        public BiddingTask LoadBiddingTaskByTenderSysNo(int tenderSysNo)
        {
            return BiddingTaskDA.LoadBiddingTaskByTenderSysNo(tenderSysNo);
            
        }


        /// <summary>
        /// 检查BiddingTask信息
        /// </summary>
        private void CheckBiddingTask(BiddingTask entity, bool isCreate)
        {
            if (!isCreate && entity.SysNo == 0)
            {
                throw new BusinessException(LangHelper.GetText("请传入数据主键！"));
            }

        }

        /// <summary>
        /// 检查招标状态，如已进入下一步，则不能对调价做任何修改
        /// </summary>
        /// <param name="tenderSysNo"></param>
        private void CheckTenderStatus(int tenderSysNo)
        {
            TenderBasicInfo tenderBasicInfo = TenderBasicInfoDA.LoadTenderBasicInfoWithoutRichTxt(tenderSysNo);
            if (tenderBasicInfo == null)
            {
                throw new BusinessException(LangHelper.GetText("不存在此采购任务！"));
            }
            //当状态是160的时候还需要判断审核状态
            if (tenderBasicInfo.Status >= TenderStatus.ConfirmFixTender)
            {
                if (tenderBasicInfo.Status == TenderStatus.ConfirmFixTender)
                {
                    //审核状态是初始、审核不通过的可以进行调价
                    if (!(tenderBasicInfo.AuditFlag == AuditFlag.Init || tenderBasicInfo.AuditFlag == AuditFlag.NoPass))
                    {
                        throw new BusinessException(
                        LangHelper.GetText("此采购任务已进入【" + tenderBasicInfo.Status.GetDescription() + "】状态并且在审核流程中，不能进行此操作！"));
                    }
                }
                else
                {
                    throw new BusinessException(
                        LangHelper.GetText("此采购任务已进入【" + tenderBasicInfo.Status.GetDescription() + "】状态，不能进行此操作！"));
                }
            }
        }
        /// <summary>
        /// 检查是否可以修改
        /// </summary>
        /// <param name="biddingTask"></param>
        /// <param name="newEndTime"></param>
        private void CheckCanSave(BiddingTask biddingTask, DateTime? newEndTime)
        {
            if (biddingTask == null || biddingTask.SysNo <= 0)
            {
                throw new BusinessException(LangHelper.GetText("投标任务（调价）不存在，请检查数据主键！"));
            }

            string userSysNoStr = DataContext.GetContextItem("UserSysNo") as string;
            int userSysNo = Convert.ToInt32(userSysNoStr);
            if (!new BidEvaluationService().CheckIsEvaluationUser(biddingTask.TenderSysNo, userSysNo))
            {
                throw new BusinessException(LangHelper.GetText("您不是评标人，不能执行调价相关操作！"));
            }

            this.CheckTenderStatus(biddingTask.TenderSysNo);

            if (biddingTask.Status != BiddingTaskStatus.Int && biddingTask.Status != BiddingTaskStatus.AuditNoPass)
            {
                throw new BusinessException(LangHelper.GetText("当前没有清单进行调价，不能保存修改！"));
            }

            if (newEndTime.HasValue && newEndTime.Value <= DateTime.Now)
            {
                throw new BusinessException(LangHelper.GetText("截止时间不能小于当前时间！"));
            }
        }

        /// <summary>
        /// 设置投标任务截止时间
        /// </summary>
        /// <param name="biddingTask"></param>
        /// <returns></returns>
        public bool SetBiddingTaskEndTime(BiddingTask biddingTask )
        {
            return this.SetBiddingTaskEndTime(biddingTask, true);
        }
        public bool SetBiddingTaskEndTime(BiddingTask biddingTask, bool isCheck)
        {
            BiddingTask task = BiddingTaskDA.LoadBiddingTask(biddingTask.SysNo);

            if(isCheck) this.CheckCanSave(task, biddingTask.EndTime);

            string userSysNoStr = DataContext.GetContextItem("UserSysNo") as string;
            int userSysNo = Convert.ToInt32(userSysNoStr);
            string userName = DataContext.GetContextItem("UserDisplayName") as string;

            task.EditUserSysNo = userSysNo;
            task.EditUserName = userName;
            task.EndTime = biddingTask.EndTime;
            task.IsMsgNotice = biddingTask.IsMsgNotice;

            return BiddingTaskDA.SetBiddingTaskEndTime(task);
        }

        /// <summary>
        /// 保存，提交，审核，发布调价信息
        /// </summary>
        /// <param name="biddingTask"></param>
        /// <returns></returns>
        public bool SubmitAuditBiddingTaskNoAudit(BiddingTask biddingTask)
        {
            using (ITransaction trans = TransactionManager.Create())
            {
                //保存截止时间(修改为提交前检测并保存，在回调的时候就不修改截止时间)
                //this.SetBiddingTaskEndTime(biddingTask);
                //提交审核
                this.SubmitAuditBiddingTaskBySysNo(biddingTask.SysNo);
                //审核成功
                this.AuditBiddingTaskCallBack(new AuditResult() { BusinessSysNo = biddingTask.SysNo.ToString(), Action = AuditAction.Approved});
                //发布调价
                this.PublishBiddingTask(biddingTask.SysNo);

                trans.Complete();
            }

            this.SendBiddingTaskMsg(biddingTask.SysNo);

            return true;
        }
        /// <summary>
        /// 调价 提交审核
        /// </summary>
        /// <param name="biddingTask"></param>
        /// <returns></returns>
        public bool SubmitAuditBiddingTask(BiddingTask biddingTask)
        {
            using (ITransaction trans = TransactionManager.Create())
            {
                //保存截止时间(修改为提交前检测并保存，在回调的时候就不修改截止时间)
                //this.SetBiddingTaskEndTime(biddingTask);
                //提交审核
                this.SubmitAuditBiddingTaskBySysNo(biddingTask.SysNo);

                trans.Complete();
            }
            return true;
        }
        /// <summary>
        /// 调价 提交审核
        /// </summary>
        /// <param name="biddingTask"></param>
        /// <returns></returns>
        private bool SubmitAuditBiddingTaskBySysNo(int sysNo)
        {
            BiddingTask task = BiddingTaskDA.LoadBiddingTask(sysNo);
            if (task == null || task.SysNo <= 0)
            {
                throw new BusinessException(LangHelper.GetText("投标任务（调价）不存在，请检查数据主键！"));
            }

            if (task.Status != BiddingTaskStatus.Int && task.Status != BiddingTaskStatus.AuditNoPass)
            {
                throw new BusinessException(LangHelper.GetText("调价状态不为：待发布或者审核不过，不能执行提交审核操作！"));
            }

            string userSysNoStr = DataContext.GetContextItem("UserSysNo") as string;
            int userSysNo = Convert.ToInt32(userSysNoStr);
            string userName = DataContext.GetContextItem("UserDisplayName") as string;

            task.EditUserSysNo = userSysNo;
            task.EditUserName = userName;
            task.Status = BiddingTaskStatus.WaitAudit;

            return BiddingTaskDA.UpdateBiddingTask(task);
        }
        /// <summary>
        /// 调价 审核回调
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool AuditBiddingTaskCallBack(AuditResult result)
        {
            int sysNo;
            int.TryParse(result.BusinessSysNo, out sysNo);
            BiddingTask task = BiddingTaskDA.LoadBiddingTask(sysNo);
            if (task == null || task.SysNo <= 0)
            {
                throw new BusinessException(LangHelper.GetText("投标任务（调价）不存在，请检查数据主键！"));
            }

            string userSysNoStr = DataContext.GetContextItem("UserSysNo") as string;
            int userSysNo = Convert.ToInt32(userSysNoStr);
            string userName = DataContext.GetContextItem("UserDisplayName") as string;

            task.EditUserSysNo = userSysNo;
            task.EditUserName = userName;

            //审核通过或者驳回的时候
            if (result.Action == AuditAction.Declined || result.Action == AuditAction.Approved)
            {
                this.CheckTenderStatus(task.TenderSysNo);

                if (task.Status != BiddingTaskStatus.WaitAudit)
                {
                    throw new BusinessException(LangHelper.GetText("调价状态不为：审批中，不能执行审核操作！"));
                }
            }
            //提交审核
            if (result.Action == AuditAction.Submited)
            {
                if (task.Status != BiddingTaskStatus.Int && task.Status != BiddingTaskStatus.AuditNoPass)
                {
                    throw new BusinessException(LangHelper.GetText("调价状态不为：待发布或者审核不过，不能执行提交审核操作！"));
                }

            }
            switch (result.Action)
            {
                //提交审核
                case AuditAction.Submited:
                    task.Status = BiddingTaskStatus.WaitAudit;
                    break;
                //审核通过
                case AuditAction.Approved:
                    task.Status = BiddingTaskStatus.AuditPass;
                    break;
                //审核不通过
                case AuditAction.Declined:
                    task.Status = BiddingTaskStatus.AuditNoPass;
                    break;
            }
            return BiddingTaskDA.UpdateBiddingTask(task);
        } 
        /// <summary>
        /// 调价 审核回调
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool AuditBiddingTaskCallBackAndAutoPublish(AuditResult result)
        {
            int sysNo;
            int.TryParse(result.BusinessSysNo, out sysNo);
            using (ITransaction trans = TransactionManager.Create())
            {
                //审核
                this.AuditBiddingTaskCallBack(result);
                if (result.Action == AuditAction.Approved)
                {
                    //发布调价
                    this.PublishBiddingTask(sysNo);
                }
                trans.Complete();
            }

            if (result.Action == AuditAction.Approved)
            {
                this.SendBiddingTaskMsg(sysNo);
            }

            return true;
        }
        /// <summary>
        /// 发布调价任务
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public bool PublishBiddingTask(int sysNo)
        {
            BiddingTask task = BiddingTaskDA.LoadBiddingTask(sysNo);
            if (task == null || task.SysNo <= 0)
            {
                throw new BusinessException(LangHelper.GetText("投标任务（调价）不存在，请检查数据主键！"));
            }

            this.CheckTenderStatus(task.TenderSysNo);

            if (task.Status != BiddingTaskStatus.AuditPass)
            {
                throw new BusinessException(LangHelper.GetText("调价状态不为：审核通过，不能执行发布操作！"));
            }

            string userSysNoStr = DataContext.GetContextItem("UserSysNo") as string;
            int userSysNo = Convert.ToInt32(userSysNoStr);
            string userName = DataContext.GetContextItem("UserDisplayName") as string;

            task.EditUserSysNo = userSysNo;
            task.EditUserName = userName;
            task.Status = BiddingTaskStatus.Active;

            return BiddingTaskDA.PublishBiddingTask(task);
        }
        /// <summary>
        /// 根据招标编号查询投标任务
        /// </summary>
        /// <param name="tenderSysNo"></param>
        /// <returns></returns>
        public List<BiddingTask> GetBiddingTasksByTenderSysNo(int tenderSysNo)
        {
            return BiddingTaskDA.GetBiddingTasksByTenderSysNo(tenderSysNo);
        }
        /// <summary>
        /// 获取当前招标调价任务
        /// </summary>
        /// <param name="biddingTaskSysNo">招标系统编号</param>
        /// <returns></returns>
        public List<BiddingTaskSupplierViewModel> GetAllBiddingTaskSupplier(int biddingTaskSysNo)
        {
            return BiddingTaskDA.GetAllBiddingTaskSupplier(biddingTaskSysNo);
        }

        /// <summary>
        /// 清单添加调价供应商
        /// </summary>
        /// <returns></returns>
        public bool AddBiddingTask(List<BiddingTaskViewActionModel> biddingTaskViewActions)
        {
            string userSysNo = DataContext.GetContextItem("UserSysNo") as string;
            int editUserSysNo = Convert.ToInt32(userSysNo);
            string editUserName = DataContext.GetContextItem("UserDisplayName") as string;
            using (ITransaction trans = TransactionManager.Create())
            {
                foreach (var biddingTaskViewAction in biddingTaskViewActions)
                {
                    CheckCreateBiddingTask(biddingTaskViewAction.TenderSysNo);
                    BiddingTaskDA.UpdateBiddingTaskItemStatus(biddingTaskViewAction.TenderSysNo,
                        biddingTaskViewAction.TableSysNo, biddingTaskViewAction.SupplierSysNos,
                        SupplierBiddingStatus.Wait,
                        SupplierBiddingStatus.Auto, editUserSysNo, editUserName);
                }
                trans.Complete();
            }
            return true;
        }
        /// <summary>
        /// 清单移除调价供应商
        /// </summary>
        /// <param name="biddingTaskViewAction"></param>
        /// <returns></returns>
        public bool RemoveBiddingTask(BiddingTaskViewActionModel biddingTaskViewAction)
        {
            BiddingTask task = BiddingTaskDA.LoadBiddingTask(biddingTaskViewAction.BiddingTaskSysNo);

            this.CheckCanSave(task, null);

            string userSysNo = DataContext.GetContextItem("UserSysNo") as string;
            int editUserSysNo = Convert.ToInt32(userSysNo);
            string editUserName = DataContext.GetContextItem("UserDisplayName") as string;

            return BiddingTaskDA.UpdateBiddingTaskItemStatus(biddingTaskViewAction.TenderSysNo, biddingTaskViewAction.TableSysNo, biddingTaskViewAction.SupplierSysNos, SupplierBiddingStatus.Auto, SupplierBiddingStatus.Wait, editUserSysNo, editUserName);
        }

        /// <summary>
        /// 检查是否可以添加调价
        /// </summary>
        /// <param name="tenderSysNo"></param>
        public void CheckCreateBiddingTask(int tenderSysNo)
        {
            string userSysNoStr = DataContext.GetContextItem("UserSysNo") as string;
            int userSysNo = Convert.ToInt32(userSysNoStr);
            if (!new BidEvaluationService().CheckIsEvaluationUser(tenderSysNo, userSysNo))
            {
                throw new BusinessException(LangHelper.GetText("您不是评标人，不能执行调价相关操作！"));
            }

            var biddingTasks = BiddingTaskDA.GetBiddingTasksByTenderSysNo(tenderSysNo);
            if (biddingTasks == null || biddingTasks.Count == 0)
            {
                throw new BusinessException(LangHelper.GetText("新一轮的投标数据还未准备好，暂时不能提交调价，请稍等两分钟后再操作！"));
            }
            //查询biddingTask是否满足BiddingTask中的Status为（未激活:None、待发布:Int、审核不通过:审核不通过）
            if (biddingTasks != null && biddingTasks.Count > 0)
            {
                var task = biddingTasks.OrderByDescending(a => a.Order).First();
                if (!(task.Status == BiddingTaskStatus.None || task.Status == BiddingTaskStatus.Int ||
                    task.Status == BiddingTaskStatus.AuditNoPass))
                {
                    throw new BusinessException(LangHelper.GetText("上一轮调价还在进行中，无法添加新一轮调价！"));
                }
            }
        }

        private string GetErrorMessageFromCheckErrorCode(int errorCode)
        {
            string errorMessage;
            switch (errorCode)
            {
                case 1:
                    errorMessage = string.Empty;
                    break;
                case -1:
                    errorMessage = "已经入定标";
                    break;
                case -2:
                    errorMessage = "已开启新一轮";
                    break;
                default:
                    errorMessage = "其他未知错误";
                    break;
            }
            return errorMessage;
        }

        /// <summary>
        /// 检查是否可以修改调价任务截止时间
        /// </summary>
        /// <param name="tenderSysNo">招标编号</param>
        /// <param name="taskSysNo">调价任务编号</param>
        /// <param name="errorMessage">错误消息</param>
        /// <returns>错误code，-1：已经入定标 -2：已开启新一轮调价 1：可以修改</returns>
        public int CheckCanChangeTaskEndTime(int tenderSysNo, int taskSysNo, out string errorMessage)
        {
            var code = BiddingTaskDA.CheckCanChangeTaskEndTime(tenderSysNo, taskSysNo);
            errorMessage = this.GetErrorMessageFromCheckErrorCode(code);

            return code;
        }
        /// <summary>
        /// 修改调价任务截止时间
        /// </summary>
        /// <param name="tenderSysNo">招标编号</param>
        /// <param name="taskSysNo">调价任务编号</param>
        /// <param name="newEndTime">新截止时间</param>
        /// <param name="errorMessage">错误消息</param>
        /// <returns>错误code，-1：已经入定标 -2：已开启新一轮调价 1：可以修改</returns>
        public int ChangeTaskEndTime(int tenderSysNo, int taskSysNo, DateTime newEndTime, out string errorMessage)
        {
            var code = BiddingTaskDA.ChangeTaskEndTime(tenderSysNo, taskSysNo, newEndTime, 0, "");
            errorMessage = this.GetErrorMessageFromCheckErrorCode(code);

            return code;
        }

        #region Private Method

        /// <summary>
        /// 调价审批通过后，发送消息给供应商
        /// </summary>
        /// <param name="biddingTask"></param>
        private void SendBiddingTaskMsg(int sysNo)
        {
            BiddingTask biddingTask = BiddingTaskDA.LoadBiddingTask(sysNo);
            if (biddingTask != null && biddingTask.SysNo > 0 && biddingTask.IsMsgNotice.HasValue && biddingTask.IsMsgNotice.Value)
            {
                var suppliers = this.GetAllBiddingTaskSupplier(biddingTask.SysNo);
                if (suppliers != null && suppliers.Count > 0)
                {
                    CommonService.Service.EmailAndSMSService sms = new CommonService.Service.EmailAndSMSService();
                    string smsContent = AppSettingManager.GetSetting("SMSTemplate", "BidEvaluationModifyPrice");
                    smsContent = string.Format(smsContent, biddingTask.TenderName);

                    string userSysNo = DataContext.GetContextItem("UserSysNo") as string;
                    int editUserSysNo = Convert.ToInt32(userSysNo);
                    string editUserName = DataContext.GetContextItem("UserDisplayName") as string;
                    var supplierTelList = suppliers.Select(e => e.LegalPersonTel).Distinct();
                    foreach (var tel in supplierTelList)
                    {
                        sms.SendSMSContentWithPriority(tel, smsContent, JC.Entity.SMSType.ModifyPriceBegin, EmailAndSMSPriority.Highest, editUserSysNo,
                        editUserName, "");
                    }
                }

            }
        }

        #endregion
    }
}
