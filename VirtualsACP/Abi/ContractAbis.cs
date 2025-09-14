namespace VirtualsAcp.Abi;

public static class ContractAbis
{
    public static readonly string AcpAbi = @"[
  {
    ""inputs"": [],
    ""stateMutability"": ""nonpayable"",
    ""type"": ""constructor""
  },
  {
    ""inputs"": [],
    ""name"": ""AccessControlBadConfirmation"",
    ""type"": ""error""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""address"",
        ""name"": ""account"",
        ""type"": ""address""
      },
      {
        ""internalType"": ""bytes32"",
        ""name"": ""neededRole"",
        ""type"": ""bytes32""
      }
    ],
    ""name"": ""AccessControlUnauthorizedAccount"",
    ""type"": ""error""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""address"",
        ""name"": ""target"",
        ""type"": ""address""
      }
    ],
    ""name"": ""AddressEmptyCode"",
    ""type"": ""error""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""address"",
        ""name"": ""account"",
        ""type"": ""address""
      }
    ],
    ""name"": ""AddressInsufficientBalance"",
    ""type"": ""error""
  },
  {
    ""inputs"": [],
    ""name"": ""FailedInnerCall"",
    ""type"": ""error""
  },
  {
    ""inputs"": [],
    ""name"": ""InvalidInitialization"",
    ""type"": ""error""
  },
  {
    ""inputs"": [],
    ""name"": ""NotInitializing"",
    ""type"": ""error""
  },
  {
    ""inputs"": [],
    ""name"": ""ReentrancyGuardReentrantCall"",
    ""type"": ""error""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""address"",
        ""name"": ""token"",
        ""type"": ""address""
      }
    ],
    ""name"": ""SafeERC20FailedOperation"",
    ""type"": ""error""
  },
  {
    ""anonymous"": false,
    ""inputs"": [
      {
        ""indexed"": true,
        ""internalType"": ""uint256"",
        ""name"": ""jobId"",
        ""type"": ""uint256""
      },
      {
        ""indexed"": false,
        ""internalType"": ""uint256"",
        ""name"": ""newBudget"",
        ""type"": ""uint256""
      }
    ],
    ""name"": ""BudgetSet"",
    ""type"": ""event""
  },
  {
    ""anonymous"": false,
    ""inputs"": [
      {
        ""indexed"": false,
        ""internalType"": ""uint256"",
        ""name"": ""jobId"",
        ""type"": ""uint256""
      },
      {
        ""indexed"": true,
        ""internalType"": ""address"",
        ""name"": ""evaluator"",
        ""type"": ""address""
      },
      {
        ""indexed"": false,
        ""internalType"": ""uint256"",
        ""name"": ""evaluatorFee"",
        ""type"": ""uint256""
      }
    ],
    ""name"": ""ClaimedEvaluatorFee"",
    ""type"": ""event""
  },
  {
    ""anonymous"": false,
    ""inputs"": [
      {
        ""indexed"": false,
        ""internalType"": ""uint256"",
        ""name"": ""jobId"",
        ""type"": ""uint256""
      },
      {
        ""indexed"": true,
        ""internalType"": ""address"",
        ""name"": ""provider"",
        ""type"": ""address""
      },
      {
        ""indexed"": false,
        ""internalType"": ""uint256"",
        ""name"": ""providerFee"",
        ""type"": ""uint256""
      }
    ],
    ""name"": ""ClaimedProviderFee"",
    ""type"": ""event""
  },
  {
    ""anonymous"": false,
    ""inputs"": [
      {
        ""indexed"": false,
        ""internalType"": ""uint64"",
        ""name"": ""version"",
        ""type"": ""uint64""
      }
    ],
    ""name"": ""Initialized"",
    ""type"": ""event""
  },
  {
    ""anonymous"": false,
    ""inputs"": [
      {
        ""indexed"": false,
        ""internalType"": ""uint256"",
        ""name"": ""jobId"",
        ""type"": ""uint256""
      },
      {
        ""indexed"": true,
        ""internalType"": ""address"",
        ""name"": ""client"",
        ""type"": ""address""
      },
      {
        ""indexed"": true,
        ""internalType"": ""address"",
        ""name"": ""provider"",
        ""type"": ""address""
      },
      {
        ""indexed"": true,
        ""internalType"": ""address"",
        ""name"": ""evaluator"",
        ""type"": ""address""
      }
    ],
    ""name"": ""JobCreated"",
    ""type"": ""event""
  },
  {
    ""anonymous"": false,
    ""inputs"": [
      {
        ""indexed"": true,
        ""internalType"": ""uint256"",
        ""name"": ""jobId"",
        ""type"": ""uint256""
      },
      {
        ""indexed"": true,
        ""internalType"": ""address"",
        ""name"": ""paymentToken"",
        ""type"": ""address""
      }
    ],
    ""name"": ""JobPaymentTokenSet"",
    ""type"": ""event""
  },
  {
    ""anonymous"": false,
    ""inputs"": [
      {
        ""indexed"": true,
        ""internalType"": ""uint256"",
        ""name"": ""jobId"",
        ""type"": ""uint256""
      },
      {
        ""indexed"": false,
        ""internalType"": ""uint8"",
        ""name"": ""oldPhase"",
        ""type"": ""uint8""
      },
      {
        ""indexed"": false,
        ""internalType"": ""uint8"",
        ""name"": ""phase"",
        ""type"": ""uint8""
      }
    ],
    ""name"": ""JobPhaseUpdated"",
    ""type"": ""event""
  },
  {
    ""anonymous"": false,
    ""inputs"": [
      {
        ""indexed"": false,
        ""internalType"": ""uint256"",
        ""name"": ""memoId"",
        ""type"": ""uint256""
      },
      {
        ""indexed"": false,
        ""internalType"": ""bool"",
        ""name"": ""isApproved"",
        ""type"": ""bool""
      },
      {
        ""indexed"": false,
        ""internalType"": ""string"",
        ""name"": ""reason"",
        ""type"": ""string""
      }
    ],
    ""name"": ""MemoSigned"",
    ""type"": ""event""
  },
  {
    ""anonymous"": false,
    ""inputs"": [
      {
        ""indexed"": true,
        ""internalType"": ""uint256"",
        ""name"": ""jobId"",
        ""type"": ""uint256""
      },
      {
        ""indexed"": true,
        ""internalType"": ""address"",
        ""name"": ""sender"",
        ""type"": ""address""
      },
      {
        ""indexed"": false,
        ""internalType"": ""uint256"",
        ""name"": ""memoId"",
        ""type"": ""uint256""
      },
      {
        ""indexed"": false,
        ""internalType"": ""string"",
        ""name"": ""content"",
        ""type"": ""string""
      }
    ],
    ""name"": ""NewMemo"",
    ""type"": ""event""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""address"",
        ""name"": ""provider"",
        ""type"": ""address""
      },
      {
        ""internalType"": ""address"",
        ""name"": ""evaluator"",
        ""type"": ""address""
      },
      {
        ""internalType"": ""uint256"",
        ""name"": ""expiredAt"",
        ""type"": ""uint256""
      }
    ],
    ""name"": ""createJob"",
    ""outputs"": [
      {
        ""internalType"": ""uint256"",
        ""name"": """",
        ""type"": ""uint256""
      }
    ],
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""uint256"",
        ""name"": ""jobId"",
        ""type"": ""uint256""
      },
      {
        ""internalType"": ""string"",
        ""name"": ""content"",
        ""type"": ""string""
      },
      {
        ""internalType"": ""enum InteractionLedger.MemoType"",
        ""name"": ""memoType"",
        ""type"": ""uint8""
      },
      {
        ""internalType"": ""bool"",
        ""name"": ""isSecured"",
        ""type"": ""bool""
      },
      {
        ""internalType"": ""uint8"",
        ""name"": ""nextPhase"",
        ""type"": ""uint8""
      }
    ],
    ""name"": ""createMemo"",
    ""outputs"": [
      {
        ""internalType"": ""uint256"",
        ""name"": """",
        ""type"": ""uint256""
      }
    ],
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""uint256"",
        ""name"": ""jobId"",
        ""type"": ""uint256""
      },
      {
        ""internalType"": ""string"",
        ""name"": ""content"",
        ""type"": ""string""
      },
      {
        ""internalType"": ""address"",
        ""name"": ""token"",
        ""type"": ""address""
      },
      {
        ""internalType"": ""uint256"",
        ""name"": ""amount"",
        ""type"": ""uint256""
      },
      {
        ""internalType"": ""address"",
        ""name"": ""recipient"",
        ""type"": ""address""
      },
      {
        ""internalType"": ""uint256"",
        ""name"": ""feeAmount"",
        ""type"": ""uint256""
      },
      {
        ""internalType"": ""enum ACPSimple.FeeType"",
        ""name"": ""feeType"",
        ""type"": ""uint8""
      },
      {
        ""internalType"": ""enum InteractionLedger.MemoType"",
        ""name"": ""memoType"",
        ""type"": ""uint8""
      },
      {
        ""internalType"": ""uint8"",
        ""name"": ""nextPhase"",
        ""type"": ""uint8""
      },
      {
        ""internalType"": ""uint256"",
        ""name"": ""expiredAt"",
        ""type"": ""uint256""
      }
    ],
    ""name"": ""createPayableMemo"",
    ""outputs"": [
      {
        ""internalType"": ""uint256"",
        ""name"": """",
        ""type"": ""uint256""
      }
    ],
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""uint256"",
        ""name"": ""memoId"",
        ""type"": ""uint256""
      },
      {
        ""internalType"": ""bool"",
        ""name"": ""isApproved"",
        ""type"": ""bool""
      },
      {
        ""internalType"": ""string"",
        ""name"": ""reason"",
        ""type"": ""string""
      }
    ],
    ""name"": ""signMemo"",
    ""outputs"": [],
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""uint256"",
        ""name"": ""jobId"",
        ""type"": ""uint256""
      },
      {
        ""internalType"": ""uint256"",
        ""name"": ""amount"",
        ""type"": ""uint256""
      }
    ],
    ""name"": ""setBudget"",
    ""outputs"": [],
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""uint256"",
        ""name"": ""jobId"",
        ""type"": ""uint256""
      },
      {
        ""internalType"": ""uint256"",
        ""name"": ""amount"",
        ""type"": ""uint256""
      },
      {
        ""internalType"": ""contract IERC20"",
        ""name"": ""jobPaymentToken_"",
        ""type"": ""address""
      }
    ],
    ""name"": ""setBudgetWithPaymentToken"",
    ""outputs"": [],
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  }
]";

    public static readonly string Erc20Abi = @"[
  {
    ""constant"": true,
    ""inputs"": [],
    ""name"": ""name"",
    ""outputs"": [{""name"": """", ""type"": ""string""}],
    ""payable"": false,
    ""stateMutability"": ""view"",
    ""type"": ""function""
  },
  {
    ""constant"": false,
    ""inputs"": [
      {""name"": ""spender"", ""type"": ""address""},
      {""name"": ""value"", ""type"": ""uint256""}
    ],
    ""name"": ""approve"",
    ""outputs"": [{""name"": """", ""type"": ""bool""}],
    ""payable"": false,
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  },
  {
    ""constant"": true,
    ""inputs"": [],
    ""name"": ""totalSupply"",
    ""outputs"": [{""name"": """", ""type"": ""uint256""}],
    ""payable"": false,
    ""stateMutability"": ""view"",
    ""type"": ""function""
  },
  {
    ""constant"": false,
    ""inputs"": [
      {""name"": ""from"", ""type"": ""address""},
      {""name"": ""to"", ""type"": ""address""},
      {""name"": ""value"", ""type"": ""uint256""}
    ],
    ""name"": ""transferFrom"",
    ""outputs"": [{""name"": """", ""type"": ""bool""}],
    ""payable"": false,
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  },
  {
    ""constant"": true,
    ""inputs"": [],
    ""name"": ""decimals"",
    ""outputs"": [{""name"": """", ""type"": ""uint8""}],
    ""payable"": false,
    ""stateMutability"": ""view"",
    ""type"": ""function""
  },
  {
    ""constant"": true,
    ""inputs"": [{""name"": ""owner"", ""type"": ""address""}],
    ""name"": ""balanceOf"",
    ""outputs"": [{""name"": ""balance"", ""type"": ""uint256""}],
    ""payable"": false,
    ""stateMutability"": ""view"",
    ""type"": ""function""
  },
  {
    ""constant"": true,
    ""inputs"": [],
    ""name"": ""symbol"",
    ""outputs"": [{""name"": """", ""type"": ""string""}],
    ""payable"": false,
    ""stateMutability"": ""view"",
    ""type"": ""function""
  },
  {
    ""constant"": false,
    ""inputs"": [
      {""name"": ""to"", ""type"": ""address""},
      {""name"": ""value"", ""type"": ""uint256""}
    ],
    ""name"": ""transfer"",
    ""outputs"": [{""name"": """", ""type"": ""bool""}],
    ""payable"": false,
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  },
  {
    ""constant"": true,
    ""inputs"": [
      {""name"": ""owner"", ""type"": ""address""},
      {""name"": ""spender"", ""type"": ""address""}
    ],
    ""name"": ""allowance"",
    ""outputs"": [{""name"": """", ""type"": ""uint256""}],
    ""payable"": false,
    ""stateMutability"": ""view"",
    ""type"": ""function""
  },
  {""payable"": true, ""stateMutability"": ""payable"", ""type"": ""fallback""},
  {
    ""anonymous"": false,
    ""inputs"": [
      {""indexed"": true, ""name"": ""owner"", ""type"": ""address""},
      {""indexed"": true, ""name"": ""spender"", ""type"": ""address""},
      {""indexed"": false, ""name"": ""value"", ""type"": ""uint256""}
    ],
    ""name"": ""Approval"",
    ""type"": ""event""
  },
  {
    ""anonymous"": false,
    ""inputs"": [
      {""indexed"": true, ""name"": ""from"", ""type"": ""address""},
      {""indexed"": true, ""name"": ""to"", ""type"": ""address""},
      {""indexed"": false, ""name"": ""value"", ""type"": ""uint256""}
    ],
    ""name"": ""Transfer"",
    ""type"": ""event""
  }
]";

    public static readonly string Eip1271Abi = @"[
  {
    ""inputs"": [
      {
        ""internalType"": ""bytes32"",
        ""name"": ""hash"",
        ""type"": ""bytes32""
      },
      {
        ""internalType"": ""bytes"",
        ""name"": ""signature"",
        ""type"": ""bytes""
      }
    ],
    ""name"": ""isValidSignature"",
    ""outputs"": [
      {
        ""internalType"": ""bytes4"",
        ""name"": """",
        ""type"": ""bytes4""
      }
    ],
    ""stateMutability"": ""view"",
    ""type"": ""function""
  }
]";

    public static readonly string SemiModularAccountAbi = @"[
  {
    ""inputs"": [
      {
        ""internalType"": ""address"",
        ""name"": ""target"",
        ""type"": ""address""
      },
      {
        ""internalType"": ""uint256"",
        ""name"": ""value"",
        ""type"": ""uint256""
      },
      {
        ""internalType"": ""bytes"",
        ""name"": ""data"",
        ""type"": ""bytes""
      }
    ],
    ""name"": ""execute"",
    ""outputs"": [],
    ""stateMutability"": ""nonpayable"",
    ""type"": ""function""
  },
  {
    ""inputs"": [
      {
        ""internalType"": ""bytes32"",
        ""name"": ""hash"",
        ""type"": ""bytes32""
      },
      {
        ""internalType"": ""bytes"",
        ""name"": ""signature"",
        ""type"": ""bytes""
      }
    ],
    ""name"": ""isValidSignature"",
    ""outputs"": [
      {
        ""internalType"": ""bytes4"",
        ""name"": """",
        ""type"": ""bytes4""
      }
    ],
    ""stateMutability"": ""view"",
    ""type"": ""function""
  }
]";
}
