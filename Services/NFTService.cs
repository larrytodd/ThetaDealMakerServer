using Nethereum.Web3;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts.CQS;
using Nethereum.Util;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Contracts;
using Nethereum.Contracts.Extensions;
using System.Numerics;
using ThetaDealMakerMVC.Models;
using ThetaDealMakerMVC.Shared;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json.Linq;
using Nethereum.Model;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Collections;
using System.Runtime.InteropServices.JavaScript;

namespace ThetaDealMakerMVC.Services;
public class NFTService{
   
    private const string _API_LINK_="https://beta-explorer-api.thetatoken.org/api/account/tokenTx/~WALLET_ADDRESS~?type=TNT-721&pageNumber=~PAGE_NUMBER~";
    public List<BasketNFT> GetNFTsInUserWallet(string address){
        var nfts = new List<BasketNFT>();
        var nftToFromCounters= new List<NFTToFromCounter>();
        int page=1;
        HttpClient client= new HttpClient();
        HttpResponseMessage response=client.GetAsync(_API_LINK_.Replace("~WALLET_ADDRESS~",address).Replace("~PAGE_NUMBER~","1")).Result;
        string result=response.Content.ReadAsStringAsync().Result;
        if(result.StartsWith("{") && result.EndsWith("}")){
               JObject o= JObject.Parse(result);
                int totalPages=(int)o["totalPageNumber"];
                while(page<=totalPages && (result.StartsWith("{") && result.EndsWith("}"))){
                    if(page>1){
                        response=client.GetAsync(_API_LINK_.Replace("~WALLET_ADDRESS~",address).Replace("~PAGE_NUMBER~",page.ToString())).Result;
                        result=response.Content.ReadAsStringAsync().Result;
                        o=JObject.Parse(result);
                    }
                    JArray items= (JArray)o["body"];
                    foreach(var item in items){
                        NFTToFromCounter counter = new NFTToFromCounter(){
                            DirectionDeterminer=item.Value<string>("to").ToLower() ==address.ToLower()?1:-1,
                             ContractAddress=item.Value<string>("contract_address"),
                             TokenId=item.Value<string>("token_id")
                        };
                        nftToFromCounters.Add(counter);
                    }
                    page++;
                }
        }
        if(nftToFromCounters.Count>0){
            nfts= nftToFromCounters.GroupBy(x=> new {x.ContractAddress, x.TokenId},(key,group)=> new {c=key.ContractAddress, t=key.TokenId,
                                            s=group.Sum(z=>z.DirectionDeterminer) }).Where(y=>y.s>0).Select(l=> new BasketNFT(){
                                                 Address=l.c,
                                                 TokenId=Convert.ToInt32(l.t)
                                            }).ToList();
        }
        return nfts;
    }
    public string GetTestNFTMetaData(int id){
        string value="";
        switch(id) {
            case 1:
                value="{\"attributes\": [{\"trait_type\": \"Shape\",\"value\": \"Circle\"},{\"trait_type\": \"Mood\",\"value\": \"Sad\"}],\"description\": \"A sad circle.\",\"image\": \"https://i.imgur.com/Qkw9N0A.jpeg\",\"name\": \"Sad Circle\"}";
                break;
            case 2:
                value="{\"attributes\": [{\"trait_type\": \"Shape\",\"value\": \"Rectangle\"},{\"trait_type\": \"Mood\",\"value\": \"Angry\"}],\"description\": \"An angry rectangle.\",\"image\": \"https://i.imgur.com/SMneO6k.jpeg\",\"name\": \"Angry Rectangle\"}";
                break;
            case 3:
                value="{\"attributes\": [{\"trait_type\": \"Shape\",\"value\": \"Triangle\"},{\"trait_type\": \"Mood\",\"value\": \"Bored\"}],\"description\": \"An bored triangle.\",\"image\": \"https://i.imgur.com/hMVRFoJ.jpeg\",\"name\": \"Bored Triangle\"}";
                break;
            default:
                value="{\"attributes\": [{\"trait_type\": \"Shape\",\"value\": \"Circle\"},{\"trait_type\": \"Mood\",\"value\": \"Sad\"}],\"description\": \"A sad circle.\",\"image\": \"https://i.imgur.com/Qkw9N0A.jpeg\",\"name\": \"Sad Circle\"}";
                break;
        };
        return value;
    }
    private class NFTToFromCounter{
        public int DirectionDeterminer {get;set;}
        public string ContractAddress{get;set;}

        public string TokenId{get;set;}
    }
    
}