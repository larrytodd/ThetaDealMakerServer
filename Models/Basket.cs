namespace ThetaDealMakerMVC.Models;
public class Basket{
    public int BasketId{get;set;}
    public int OfferId{get;set;}
    public string? BasketName{get;set;}
    public bool IsActive{get;set;}
    public string? Owner {get;set;}
    public List<BasketNFT>? NftsInBasket {get; set;}
    public List<BasketNFT>? NftsInWallet {get;set;}
    public List<BasketOffer>? OffersMade {get;set;}
    public List<BasketOffer>? OffersReceived {get; set;}
    public decimal? TFuel {get;set;}
    public DateTime EnterDate{get;set;}
    public DateTime UpdateDate{get;set;}
}