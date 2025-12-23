namespace FourMProductSyncPlugin;

public enum AssetApprovalStatus
{
     /// <summary>
    /// Đề xuất nghiệm thu
    /// </summary>

    ProposedAcceptance,

    /// <summary>
    /// Đề xuất sửa chữa
    /// </summary>
  
    ProposedRepairRequest,

    /// <summary>
    /// Đề xuất thanh lý
    /// </summary>
 
    ProposedLiquidate,

    /// <summary>
    /// Đã duyệt
    /// </summary>

    Approved,

    /// <summary>
    /// Đang sử dụng
    /// </summary>
 
    Using,

    /// <summary>
    /// Đã từ chối
    /// </summary>
    Rejected,

    /// <summary>
    /// Đã thanh lý
    /// </summary>
    Liquidated,
    
    /// <summary>
    /// không được sử dụng
    /// </summary>
    NotInUse
}