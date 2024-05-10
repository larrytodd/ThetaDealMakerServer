namespace ThetaDealMakerMVC.Models;

public class BasketNFT{
    public int BasketNFTId {get; set;}
    public int BasketId {get;set;}
    public int NFTId {get;set;}
    public string? Name {get; set;}
    public string? ImageURL{get;set;}
    public int TokenId{get;set;}
    public string? Address {get; set;}
}