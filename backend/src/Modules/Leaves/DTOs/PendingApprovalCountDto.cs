namespace taskedin_be.src.Modules.Leaves.DTOs;

public class PendingApprovalCountDto
{
    public int PendingManagerApproval { get; set; }
    public int PendingHRApproval { get; set; }
    public int TotalPending { get; set; }
}
