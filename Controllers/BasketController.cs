using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ThetaDealMakerMVC.Models;
using ThetaDealMakerMVC.Services;
using System.Text.Json;

namespace ThetaDealMakerMVC.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BasketController : ControllerBase
{
    BasketService basketService;
    public BasketController(){
        this.basketService=new BasketService();
    }
    [HttpPost("Save")]
    public IActionResult SaveBasket(Basket model){
        try{
            basketService.InsertBasket(model);
        }catch(Exception ex){
            return StatusCode(StatusCodes.Status500InternalServerError,ex.Message);
        }
        return new JsonResult("{\"Success\":true}"); 
    }

    [HttpPost("AddNft")]
    public IActionResult AddNFT(BasketNFT model){
        int basketNftId=0;
        try{
         basketNftId= basketService.InsertBasketNFT(model);
        }catch(Exception ex){
            return StatusCode(StatusCodes.Status500InternalServerError,ex.Message);
        }
        return new JsonResult("{\"Success\":true,\"Id\":"+basketNftId.ToString()+"}"); 
    }
    [HttpPost("UpdateTfuelInBasket")]
    public IActionResult UpdateTfuelInBasket(Basket updateBasket){
        try{
            basketService.UpdateTfuelInBasket(updateBasket.TFuel.HasValue?updateBasket.TFuel.Value:0.00M, updateBasket.BasketId);
        }catch(Exception ex){
            return StatusCode(StatusCodes.Status500InternalServerError,ex.Message);
        }
         return new JsonResult("{\"Success\":true}"); 
    }
    [HttpPost("UpdateBasketActiveInactive")]
    public IActionResult UpdateBasketActiveInactive(Basket updateBasket){
        try{
            basketService.UpdateBasketActiveInactive(updateBasket.IsActive, updateBasket.BasketId);
        }catch(Exception ex){
            return StatusCode(StatusCodes.Status500InternalServerError,ex.Message);
        }
         return new JsonResult("{\"Success\":true}"); 
    }
    [HttpGet("GetBasket/{id?}/{addr?}")]
    public IActionResult GetBasketById(int id=0,string addr=""){
        var basket = basketService.GetBasket(id,addr);
        return new JsonResult(basket);
    }
    [HttpGet("GetBasketsByOwner/{owner}")]
    public IActionResult GetBasketsByOwner(string owner){
        var baskets= basketService.GetBasketsByOwner(owner);
        return new JsonResult(baskets);
    }
    [HttpGet("GetBasketsByRecency/{indexStart}/{indexEnd}/{isActive?}")]
    public IActionResult GetBasketsByRecency(int indexStart, int indexEnd,bool? isActive=null){
        if((indexEnd-indexStart)>25)
        {
            return StatusCode(StatusCodes.Status428PreconditionRequired,"Maximum amount of items to request is 25.");
        }
        var baskets= basketService.GetLastestBaskets(indexStart,indexEnd,isActive);
        return new JsonResult(baskets);
    }
     [HttpGet("SearchBaskets/{searchTerm}")]
    public IActionResult SearchBaskets(string searchTerm){
        searchTerm= searchTerm.Length>50?searchTerm.Substring(0,50):searchTerm;
        var baskets= basketService.SearchBaskets(searchTerm);
        return new JsonResult(baskets);
    }
    [HttpPost("RemoveNft")]
    public IActionResult RemoveNFT([FromBody]int id)
    {
        try{
            basketService.RemoveBasketNFT(id);
        }catch(Exception ex){
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
        return new JsonResult("{\"Success\":true}");
    }
    [HttpPost("RemoveBasket")]
    public IActionResult RemoveBasket([FromBody]int id){
        try{
            basketService.RemoveBasket(id);
        }catch(Exception ex){
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
        return new JsonResult("{\"Success\":true}");
    }
    [HttpPost("AddBasketOffer")]
    public IActionResult AddBasketOffer(BasketOffer model){
        int offerId=0;
        try{
         offerId= basketService.InsertBasketOffer(model);

        }catch(Exception ex){
            return StatusCode(StatusCodes.Status500InternalServerError,ex.Message);
        }
        var offer=basketService.GetBasketOfferById(offerId);
        
        var offerJsonString= JsonSerializer.Serialize(offer, new JsonSerializerOptions(){PropertyNamingPolicy=null});
        return new JsonResult("{\"Success\":true,\"Offer\":"+ offerJsonString +"}"); 
    }
    [HttpPost("RemoveOffer")]
    public IActionResult RemoveOffer([FromBody]int id){
        try{
            basketService.RemoveBasketOffer(id);
        }catch(Exception ex){
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
        return new JsonResult("{\"Success\":true}");
    }
    [HttpPost("UpdateOfferAccepted")]
    public IActionResult UpdateOfferAccepted(BasketOffer model){
        try{
            basketService.UpdateBasketOfferAccepted(model.Accepted,model.OfferId);
        }catch(Exception ex){
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
        return new JsonResult("{\"Success\":true}");
    }
}