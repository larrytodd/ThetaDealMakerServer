using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ThetaDealMakerMVC.Models;
using ThetaDealMakerMVC.Services;

namespace ThetaDealMakerMVC.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

//async Task<IActionResult>
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    [Route("nft_meta_data/{id?}")]
    public IActionResult GetTestNFTMetaData(int id=0){
        //Deployed to: 0xB79f3bc89B562349BF7a5B1F40e6FDd027C7783A
        var nftService= new NFTService();
        return this.Content(nftService.GetTestNFTMetaData(id), "application/json", System.Text.Encoding.UTF8);
    }
}
