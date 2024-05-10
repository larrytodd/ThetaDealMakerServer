namespace ThetaDealMakerMVC.Models;

public class BasketOffer
{
    public int OfferId{get; set;}
    public string? OfferedOwner {get;set;}
    public string? OfferedOnOwner {get;set;}
    public int BasketOfferedOn {get;set;}
    public int BasketOffered{get;set;}
    public string? BasketOfferedOnName{get;set;}
    public List<BasketNFT>? BasketOfferedOnNFTs{get; set;}
    public decimal? BasketOfferedOnTFuel {get;set;}
    public string? BasketOfferedName{get;set;}
    public List<BasketNFT>? BasketOfferedNFTs{get; set;}
    public decimal? BasketOfferedTFuel {get;set;}
    public bool Accepted{get;set;}
    public DateTime EnterDate{get;set;}  

}