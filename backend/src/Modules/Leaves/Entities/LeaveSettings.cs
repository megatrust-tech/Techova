using taskedin_be.src.Modules.Common.Entities;

namespace taskedin_be.src.Modules.Leaves.Entities
{
    public class LeaveTypeConfig : BaseEntity
    {
        public int LeaveTypeId { get; set; }
        public string Name { get; set; } = string.Empty;

        public int DefaultBalance { get; set; } = 21;

        public bool AutoApproveEnabled { get; set; } = false;
        public int AutoApproveThresholdDays { get; set; } = 0;
        
        /// <summary>
        /// When true, auto-approved requests skip conflict checking.
        /// Useful for Emergency leave types.
        /// </summary>
        public bool BypassConflictCheck { get; set; } = false;
    }
}