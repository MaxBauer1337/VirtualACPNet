namespace VirtualsAcp.Models;

public enum AcpMemoStatus
{
    Pending,
    Approved,
    Rejected,
    Expired
}

public enum MemoType
{
    Message = 0,
    ContextUrl = 1,
    ImageUrl = 2,
    VoiceUrl = 3,
    ObjectUrl = 4,
    TxHash = 5,
    PayableRequest = 6,
    PayableTransfer = 7,
    PayableTransferEscrow = 8
}

public enum AcpJobPhase
{
    Request = 0,
    Negotiation = 1,
    Transaction = 2,
    Evaluation = 3,
    Completed = 4,
    Rejected = 5,
    Expired = 6
}

public enum FeeType
{
    NoFee = 0,
    ImmediateFee = 1,
    DeferredFee = 2
}

public enum AcpAgentSort
{
    successfulJobCount,
    successRate,
    uniqueBuyerCount,
    minsFromLastOnline
}

public enum AcpGraduationStatus
{
    Graduated,
    NotGraduated,
    All
}

public enum AcpOnlineStatus
{
    Online,
    Offline,
    All
}

public enum PayloadType
{
    FundResponse,
    OpenPosition,
    ClosePosition,
    ClosePartialPosition,
    PositionFulfilled,
    CloseJobAndWithdraw,
    UnfulfilledPosition
}

public enum PositionDirection
{
    Long,
    Short
}
