﻿namespace SwapHunter.Client
{
  public class TibetSwapOptions
  {
    public string ApiEndpoint { get; set; }
    public string TokensEndpoint { get; set; }
    public int TokenRefreshDelay { get; set; }
    public string TokenPairEndpoint { get; set; }
    public string QuoteEndpoint { get; set; }
  }
} 