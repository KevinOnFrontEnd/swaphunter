﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwapHunter.Client
{
  public interface ITibetClient
  {
    Task<List<TokenResponse>> GetTokenPairs();
    Task<TokenPairResponse> GetPair(string pair);
    Task<QuoteResponse> GetQuote(string pair, string amount_in, bool xch_is_input = true, bool estimate_fee = true);
  }
}