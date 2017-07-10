using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace YZ.JC.Entity
{
    public enum AuditFlowStatus
    {
        [Description("草稿")]
        Draft = 0,
        [Description("审批中")]
        InApproval = 1,
        [Description("通过")]
        Passed = 2,
        [Description("驳回")]
        Failed = 3,
        [Description("撤销")]
        Undo = 4
    }

    public enum AuditRecordStatus
    {
        [Description("审批中")]
        Pending = 0,
        [Description("通过")]
        Passed = 1,
        [Description("驳回")]
        Failed = 2,
        [Description("撤销")]
        Undo = 3,
        [Description("未审批")]
        NoAudited = 4,
    }

    /// <summary>
    /// 是否被驳回
    /// </summary>
    public enum AsFailed
    {
        [Description("否")]
        No = 0,
        [Description("是")]
        Yes = 2
    }
}
