using System;
using System.Data.SqlClient;
using System.Linq;
using System.Data;
using ThetaDealMakerMVC.Models;
using System.Runtime.CompilerServices;
using Org.BouncyCastle.Cms;
using Newtonsoft.Json.Linq;

namespace ThetaDealMakerMVC.Services;
public class BasketService{
    string connString= new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional:true, reloadOnChange:true)
                    .Build().GetConnectionString("DefaultConnection")??"";
                    
    private const string _CONTRACT_API_="https://beta-explorer-api.thetatoken.org/api/smartcontract/~ADDRESS~";
    NFTService nftService= new NFTService();
    public void InsertBasket(Basket basket){
        using(SqlConnection connection = new SqlConnection(connString) ){
            using(SqlCommand cmd = connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= System.Data.CommandType.StoredProcedure;
                cmd.CommandText="InsertBasket";
                cmd.Parameters.AddWithValue("@BasketId", basket.BasketId);
                cmd.Parameters.AddWithValue("@Owner",basket.Owner);
                cmd.Parameters.AddWithValue("@BasketName",basket.BasketName);
                cmd.ExecuteNonQuery();
            }
        } 
    }
    public int InsertBasketNFT(BasketNFT basketNFT){
        string contractName="";
       
        HttpClient client = new HttpClient();
        HttpResponseMessage response= client.GetAsync(_CONTRACT_API_.Replace("~ADDRESS~",basketNFT.Address)).Result;
        string result=response.Content.ReadAsStringAsync().Result;
        if(result.StartsWith("{") && result.EndsWith("}")){
            JObject o= JObject.Parse(result);
            contractName = (string)o["body"]["name"]??"";
        }


        int basketNftId=0;
        using(SqlConnection connection = new SqlConnection(connString) ){
            using(SqlCommand cmd = connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= CommandType.StoredProcedure;
                cmd.CommandText="InsertBasketNFT";
                cmd.Parameters.AddWithValue("@BasketId", basketNFT.BasketId);
                cmd.Parameters.AddWithValue("@Address",basketNFT.Address);
                cmd.Parameters.AddWithValue("@Name",(basketNFT.Name?.Length>100?basketNFT.Name?.Substring(0,95)+"...":basketNFT?.Name));
                cmd.Parameters.AddWithValue("@ContractName",(contractName.Length>100?contractName.Substring(0,95)+"...":contractName));
                cmd.Parameters.AddWithValue("@ImageURL",basketNFT?.ImageURL);
                cmd.Parameters.AddWithValue("@TokenId",basketNFT?.TokenId);
                SqlParameter parameter = new SqlParameter("@BasketNftId",SqlDbType.Int);
                parameter.Direction= ParameterDirection.Output;
                cmd.Parameters.Add(parameter);
                cmd.ExecuteNonQuery();
                basketNftId = (int)cmd.Parameters["@BasketNftId"].Value;
            }
        }
        return basketNftId;
    }
    public void RemoveBasketNFT(int id){
        using(SqlConnection connection= new SqlConnection(connString)){
            using(SqlCommand cmd = connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= CommandType.StoredProcedure;
                cmd.CommandText="DeleteBasketNFT";
                cmd.Parameters.AddWithValue("@BasketNftId", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
    public void RemoveBasket(int id){
        using(SqlConnection connection= new SqlConnection(connString)){
            using(SqlCommand cmd = connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= CommandType.StoredProcedure;
                cmd.CommandText="DeleteBasketNftsByBasketId";
                cmd.Parameters.AddWithValue("@BasketId", id);
                cmd.ExecuteNonQuery();
            }
        }
        using(SqlConnection connection= new SqlConnection(connString)){
            using(SqlCommand cmd = connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= CommandType.StoredProcedure;
                cmd.CommandText="DeleteBasket";
                cmd.Parameters.AddWithValue("@BasketId", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
    public Basket GetBasket(int id,string addressRequesting){
        Basket basket=new Basket();
        using(SqlConnection connection= new SqlConnection(connString)){
            using(SqlCommand cmd= connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= System.Data.CommandType.StoredProcedure;
                cmd.CommandText="GetBasketById";
                cmd.Parameters.AddWithValue("@BasketId",id);
                using(var reader= cmd.ExecuteReader()){
                    if(reader.Read()){
                        basket.BasketId=id;
                        basket.BasketName=reader["BasketName"].ToString();
                        basket.IsActive=(bool)reader["IsActive"];
                        basket.TFuel=(decimal)reader["TFuel"];
                        basket.Owner=reader["Owner"].ToString();
                        basket.EnterDate=(DateTime)reader["EnterDate"];
                        basket.UpdateDate=(DateTime)reader["UpdateDate"];
                    }
                }
            }
        }
        using(SqlConnection connection=new SqlConnection(connString)){
            using(SqlCommand cmd= connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= System.Data.CommandType.StoredProcedure;
                cmd.CommandText="GetBasketNftsByBasketId";
                cmd.Parameters.AddWithValue("@BasketId", id);
                using(var reader=cmd.ExecuteReader()){
                    var dt= new DataTable();
                    dt.Load(reader);
                    basket.NftsInBasket=dt.AsEnumerable().Select(x=>new BasketNFT(){
                        BasketNFTId=x.Field<int>("BasketNftId")
                        ,BasketId=id
                        ,NFTId=x.Field<int>("NFTId")
                        ,Name=x.Field<string>("Name")
                        ,ImageURL=x.Field<string>("ImageUrl")
                        ,TokenId= x.Field<int>("TokenId")
                        ,Address=x.Field<string>("Address")
                    }).ToList()??new List<BasketNFT>();
                }
            }
        }
        Dictionary<int,BasketOffer> basketToOffered= new Dictionary<int, BasketOffer>();
        using(SqlConnection connection = new SqlConnection(connString)){
            using(SqlCommand cmd= connection.CreateCommand()){
                connection.Open();
                 cmd.CommandType= System.Data.CommandType.StoredProcedure;
                cmd.CommandText="getBasketOffersMadeByBasketId";
                cmd.Parameters.AddWithValue("@BasketId", id);
                using(var reader= cmd.ExecuteReader()){
                    while(reader.Read()){
                        if(!basketToOffered.ContainsKey((int)reader["OfferId"])){
                        basketToOffered.Add((int)reader["OfferId"], new BasketOffer(){
                                OfferId=(int)reader["OfferId"],
                                BasketOffered=(int)reader["BasketId"],
                                OfferedOwner=reader["Owner"].ToString(),
                                BasketOfferedOn=(int)reader["BasketOfferedOn"],
                                BasketOfferedOnName=reader["BasketOfferedOnName"].ToString(),
                                BasketOfferedOnTFuel=(decimal)reader["BasketOfferedOnTfuel"],
                                BasketOfferedOnNFTs= Convert.IsDBNull(reader["BasketNftId"])?null:
                                new List<BasketNFT>(){
                                    new BasketNFT(){
                                        BasketId=(int)reader["BasketOfferedOn"],
                                        BasketNFTId=(int)reader["BasketNftId"],
                                        Name=reader["Name"].ToString(),
                                        NFTId=(int)reader["NFTId"],
                                        TokenId=(int)reader["TokenId"]
                                    }
                                } 

                            });
                      }else{
                        basketToOffered[(int)reader["OfferId"]]?.BasketOfferedOnNFTs?.Add( new BasketNFT(){
                                        BasketId=(int)reader["BasketOfferedOn"],
                                        BasketNFTId=(int)reader["BasketNftId"],
                                        Name=reader["Name"].ToString(),
                                        NFTId=(int)reader["NFTId"],
                                        TokenId=(int)reader["TokenId"]
                                    });
                      }
                    }
                }
            }
        }
        basket.OffersMade= basketToOffered.Select(z=>new BasketOffer{
                OfferId=z.Value.OfferId,
                BasketOffered=z.Value.BasketOffered,
                BasketOfferedOn=z.Value.BasketOfferedOn,
                OfferedOwner=z.Value.OfferedOwner,
                BasketOfferedOnName=z.Value.BasketOfferedOnName,
                BasketOfferedOnTFuel=z.Value.BasketOfferedOnTFuel,
                BasketOfferedOnNFTs=z.Value.BasketOfferedOnNFTs
            }).ToList()??new List<BasketOffer>();

        Dictionary<int,BasketOffer> basketOffered= new Dictionary<int, BasketOffer>();
        using(SqlConnection connection= new SqlConnection(connString)){
            using(SqlCommand cmd=connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= System.Data.CommandType.StoredProcedure;
                //TODO: This SP is messed up. Need to go through and make sure I am bringing back what I want.
                cmd.CommandText="GetBasketOffersByBasketId";
                cmd.Parameters.AddWithValue("@BasketId", id);
                using(var reader= cmd.ExecuteReader()){
                    while(reader.Read()){
                        if(!basketOffered.ContainsKey((int)reader["OfferId"])){
                        basketOffered.Add((int)reader["OfferId"], new BasketOffer(){
                                OfferId=(int)reader["OfferId"],
                                BasketOffered=(int)reader["BasketOffered"],
                                BasketOfferedOn= (int)reader["BasketId"],
                                BasketOfferedName=reader["BasketName"].ToString(),
                                BasketOfferedTFuel=(decimal)reader["TFuel"],
                                BasketOfferedNFTs= Convert.IsDBNull(reader["BasketNftId"])?null:
                                new List<BasketNFT>(){
                                    new BasketNFT(){
                                        BasketId=(int)reader["BasketOffered"],
                                        BasketNFTId=(int)reader["BasketNftId"],
                                        Name=reader["Name"].ToString(),
                                        NFTId=(int)reader["NFTId"],
                                        TokenId=(int)reader["TokenId"]
                                    }
                                } 

                            });
                      }else{
                        basketOffered[(int)reader["OfferId"]]?.BasketOfferedNFTs?.Add( new BasketNFT(){
                                        BasketId=(int)reader["BasketOffered"],
                                        BasketNFTId=(int)reader["BasketNftId"],
                                        Name=reader["Name"].ToString(),
                                        NFTId=(int)reader["NFTId"],
                                        TokenId=(int)reader["TokenId"]
                                    });
                      }
                    }
                }
            }
        }
        basket.OffersReceived=basketOffered.Select(z=>new BasketOffer{
                OfferId=z.Value.OfferId,
                BasketOffered=z.Value.BasketOffered,
                BasketOfferedOn=z.Value.BasketOfferedOn,
                BasketOfferedName=z.Value.BasketOfferedName,
                BasketOfferedTFuel=z.Value.BasketOfferedTFuel,
                BasketOfferedNFTs=z.Value.BasketOfferedNFTs
            }).ToList()??new List<BasketOffer>();
        basket.NftsInWallet= (addressRequesting.ToLower()==basket?.Owner?.ToLower())? nftService.GetNFTsInUserWallet(basket.Owner) : new List<BasketNFT>();
        return basket??(new Basket());
    }
    public void UpdateTfuelInBasket(decimal tfuelAmount, int basketId){
         using(SqlConnection connection= new SqlConnection(connString)){
            using(SqlCommand cmd = connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= CommandType.StoredProcedure;
                cmd.CommandText="UpdateTfuelInBasket";
                cmd.Parameters.AddWithValue("@TFUel", tfuelAmount);
                cmd.Parameters.AddWithValue("@BasketId", basketId);
                cmd.ExecuteNonQuery();
            }
        }
    }
    public void UpdateBasketActiveInactive(bool isActive, int basketId){
        using(SqlConnection connection= new SqlConnection(connString)){
            using(SqlCommand cmd = connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= CommandType.StoredProcedure;
                cmd.CommandText="UpdateBasketActiveInactive";
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                cmd.Parameters.AddWithValue("@BasketId", basketId);
                cmd.ExecuteNonQuery();
            }
        }
    }
    public List<Basket> SearchBaskets(string searchTerm){
        List<Basket> baskets= new List<Basket>();
          Basket basket=null;
         using(SqlConnection connection= new SqlConnection(connString)){
            using(SqlCommand cmd= connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= System.Data.CommandType.StoredProcedure;
                cmd.CommandText="SearchItems";
                cmd.Parameters.AddWithValue("@SearchTerm", searchTerm);
                
                 using(var reader=cmd.ExecuteReader()){
                    while(reader.Read()){
                        if(basket==null || basket.BasketId!= (int)reader["BasketId"]){
                            if(basket!=null){
                                baskets.Add(basket);
                            }
                            basket= new Basket(){BasketId= (int)reader["BasketId"], 
                            BasketName=reader["BasketName"].ToString(),
                            Owner=reader["Owner"].ToString(),
                            EnterDate=Convert.ToDateTime(reader["EnterDate"]),
                            IsActive= (byte)reader["IsActive"]==1,
                            TFuel=(decimal)reader["TFuel"],
                            NftsInBasket= new List<BasketNFT>()};
                        }
                        if(!Convert.IsDBNull(reader["BasketNftId"])){
                             basket?.NftsInBasket?.Add(new BasketNFT(){
                                BasketNFTId=(int)reader["BasketNftId"],
                                NFTId=(int)reader["NFTId"],
                                Address = reader["Address"].ToString(),
                                ImageURL=reader["ImageUrl"].ToString(),
                                Name=reader["Name"].ToString(),
                                TokenId=(int)reader["TokenId"]
                            });
                        }
                    };
                    if(basket!=null){
                        baskets.Add(basket);
                    }
                 }
            }
        }
         return baskets;
    }
    public List<Basket> GetLastestBaskets(int start, int end, bool? isActive=null){
         List<Basket> baskets= new List<Basket>();
          Basket basket=null;
         using(SqlConnection connection= new SqlConnection(connString)){
            using(SqlCommand cmd= connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= System.Data.CommandType.StoredProcedure;
                cmd.CommandText="GetLatestBasketNumIndex";
                cmd.Parameters.AddWithValue("@MinNumIndex", start);
                cmd.Parameters.AddWithValue("@MaxNumIndex", end);
                if(isActive.HasValue){
                    cmd.Parameters.AddWithValue("@IsActive",isActive);
                }
                 using(var reader=cmd.ExecuteReader()){
                    while(reader.Read()){
                        if(basket==null || basket.BasketId!= (int)reader["BasketId"]){
                            if(basket!=null){
                                baskets.Add(basket);
                            }
                            basket= new Basket(){BasketId= (int)reader["BasketId"], 
                            BasketName=reader["BasketName"].ToString(),
                            Owner=reader["Owner"].ToString(),
                            EnterDate=Convert.ToDateTime(reader["EnterDate"]),
                            IsActive= (bool)reader["IsActive"],
                            TFuel=(decimal)reader["TFuel"],
                            NftsInBasket= new List<BasketNFT>()};
                        }
                        if(!Convert.IsDBNull(reader["BasketNftId"])){
                             basket?.NftsInBasket?.Add(new BasketNFT(){
                                BasketNFTId=(int)reader["BasketNftId"],
                                NFTId=(int)reader["NFTId"],
                                Address = reader["Address"].ToString(),
                                ImageURL=reader["ImageUrl"].ToString(),
                                Name=reader["Name"].ToString(),
                                TokenId=(int)reader["TokenId"]
                            });
                        }
                    };
                    if(basket!=null){
                        baskets.Add(basket);
                    }
                 }
            }
        }
         return baskets;
    }
    public List<Basket> GetBasketsByOwner(string owner){
        List<Basket> baskets= new List<Basket>();
        Basket basket=null;
        using(SqlConnection connection= new SqlConnection(connString)){
            using(SqlCommand cmd= connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= System.Data.CommandType.StoredProcedure;
                cmd.CommandText="GetBasketsByOwner";
                cmd.Parameters.AddWithValue("@Owner", owner);
                 using(var reader=cmd.ExecuteReader()){
                    while(reader.Read()){
                        if(basket==null || basket.BasketId!= (int)reader["BasketId"]){
                            if(basket!=null){
                                baskets.Add(basket);
                            }
                            basket= new Basket(){BasketId= (int)reader["BasketId"], 
                            BasketName=reader["BasketName"].ToString(),
                            EnterDate=Convert.ToDateTime(reader["EnterDate"]),
                            IsActive= (bool)reader["IsActive"],
                            TFuel=(decimal)reader["TFuel"],
                            NftsInBasket= new List<BasketNFT>()};
                        }
                        if(!Convert.IsDBNull(reader["BasketNftId"])){
                             basket?.NftsInBasket?.Add(new BasketNFT(){
                                BasketNFTId=(int)reader["BasketNftId"],
                                NFTId=(int)reader["NFTId"],
                                Address = reader["Address"].ToString(),
                                ImageURL=reader["ImageUrl"].ToString(),
                                Name=reader["Name"].ToString(),
                                TokenId=(int)reader["TokenId"]
                            });
                        }
                    };
                    if(basket!=null){
                        baskets.Add(basket);
                    }
                 }
            }
        }
        
        Dictionary<int,BasketOffer> basketToOffered= new Dictionary<int, BasketOffer>();
        using(SqlConnection conn= new SqlConnection(connString)){
            using(SqlCommand cmd= conn.CreateCommand()){
                conn.Open();
                cmd.CommandType= System.Data.CommandType.StoredProcedure;
                cmd.CommandText="GetBasketsOfferedOnByOwner";
                cmd.Parameters.AddWithValue("@Owner", owner);
                using(var reader= cmd.ExecuteReader()){
                    while(reader.Read()){
                      if(!basketToOffered.ContainsKey((int)reader["OfferId"])){
                        basketToOffered.Add((int)reader["OfferId"], new BasketOffer(){
                                OfferId=(int)reader["OfferId"],
                                BasketOffered=(int)reader["BasketId"],
                                BasketOfferedOn=(int)reader["BasketOfferedOn"],
                                BasketOfferedOnName=reader["BasketOfferedOnName"].ToString(),
                                BasketOfferedOnTFuel=(decimal)reader["BasketOfferedOnTfuel"],
                                BasketOfferedOnNFTs= Convert.IsDBNull(reader["BasketNftId"])?null:
                                new List<BasketNFT>(){
                                    new BasketNFT(){
                                        BasketId=(int)reader["BasketOfferedOn"],
                                        BasketNFTId=(int)reader["BasketNftId"],
                                        Name=reader["Name"].ToString(),
                                        NFTId=(int)reader["NFTId"],
                                        TokenId=(int)reader["TokenId"]
                                    }
                                } 

                            });
                      }else{
                        basketToOffered[(int)reader["OfferId"]]?.BasketOfferedOnNFTs?.Add( new BasketNFT(){
                                        BasketId=(int)reader["BasketOfferedOn"],
                                        BasketNFTId=(int)reader["BasketNftId"],
                                        Name=reader["Name"].ToString(),
                                        NFTId=(int)reader["NFTId"],
                                        TokenId=(int)reader["TokenId"]
                                    });
                      }
                    }
                }
            }
        }
        baskets.ForEach(x=>{
            x.OffersMade=basketToOffered.Where(y=>y.Value.BasketOffered==x.BasketId).Select(z=>new BasketOffer{
                OfferId=z.Value.OfferId,
                BasketOffered=z.Value.BasketOffered,
                BasketOfferedOn=z.Value.BasketOfferedOn,
                BasketOfferedOnName=z.Value.BasketOfferedOnName,
                BasketOfferedOnTFuel=z.Value.BasketOfferedOnTFuel,
                BasketOfferedOnNFTs=z.Value.BasketOfferedOnNFTs
            }).ToList()??new List<BasketOffer>();
        });

        Dictionary<int,BasketOffer> basketOffered= new Dictionary<int, BasketOffer>();
        using(SqlConnection conn= new SqlConnection(connString)){
            using(SqlCommand cmd= conn.CreateCommand()){
                conn.Open();
                cmd.CommandType= System.Data.CommandType.StoredProcedure;
                cmd.CommandText="GetBasketsOfferedToByOwner";
                cmd.Parameters.AddWithValue("@Owner", owner);
                using(var reader= cmd.ExecuteReader()){
                    while(reader.Read()){
                      if(!basketOffered.ContainsKey((int)reader["OfferId"])){
                        basketOffered.Add((int)reader["OfferId"], new BasketOffer(){
                                OfferId=(int)reader["OfferId"],
                                BasketOffered=(int)reader["BasketOffered"],
                                BasketOfferedOn= (int)reader["BasketId"],
                                BasketOfferedName=reader["BasketOfferedName"].ToString(),
                                BasketOfferedTFuel=(decimal)reader["BasketOfferedTfuel"],
                                BasketOfferedNFTs= Convert.IsDBNull(reader["BasketNftId"])?null:
                                new List<BasketNFT>(){
                                    new BasketNFT(){
                                        BasketId=(int)reader["BasketOffered"],
                                        BasketNFTId=(int)reader["BasketNftId"],
                                        Name=reader["Name"].ToString(),
                                        NFTId=(int)reader["NFTId"],
                                        TokenId=(int)reader["TokenId"]
                                    }
                                } 

                            });
                      }else{
                        basketOffered[(int)reader["OfferId"]]?.BasketOfferedNFTs?.Add( new BasketNFT(){
                                        BasketId=(int)reader["BasketOffered"],
                                        BasketNFTId=(int)reader["BasketNftId"],
                                        Name=reader["Name"].ToString(),
                                        NFTId=(int)reader["NFTId"],
                                        TokenId=(int)reader["TokenId"]
                                    });
                      }
                    }
                }
            }
        }
        baskets.ForEach(x=>{
            x.OffersReceived=basketOffered.Where(y=>y.Value.BasketOfferedOn==x.BasketId).Select(z=>new BasketOffer{
                OfferId=z.Value.OfferId,
                BasketOffered=z.Value.BasketOffered,
                BasketOfferedOn=z.Value.BasketOfferedOn,
                BasketOfferedName=z.Value.BasketOfferedName,
                BasketOfferedTFuel=z.Value.BasketOfferedTFuel,
                BasketOfferedNFTs=z.Value.BasketOfferedNFTs
            }).ToList()??new List<BasketOffer>();
        });

        return baskets;
    }

    public int InsertBasketOffer(BasketOffer basketOffer){
        int offerId=0;
        using(SqlConnection connection = new SqlConnection(connString) ){
            using(SqlCommand cmd = connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= CommandType.StoredProcedure;
                cmd.CommandText="InsertBasketOffer";
                cmd.Parameters.AddWithValue("@BasketOfferedOn", basketOffer.BasketOfferedOn);
                cmd.Parameters.AddWithValue("@BasketOffered",basketOffer.BasketOffered);
                cmd.Parameters.AddWithValue("@Accepted",basketOffer.Accepted);
                SqlParameter parameter = new SqlParameter("@OfferId",SqlDbType.Int);
                parameter.Direction= ParameterDirection.Output;
                cmd.Parameters.Add(parameter);
                cmd.ExecuteNonQuery();
                offerId = (int)cmd.Parameters["@OfferId"].Value;
            }
        }
        return offerId;
    }
     public void RemoveBasketOffer(int id){
        using(SqlConnection connection= new SqlConnection(connString)){
            using(SqlCommand cmd = connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= CommandType.StoredProcedure;
                cmd.CommandText="DeleteBasketOffer";
                cmd.Parameters.AddWithValue("@OfferId", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
    public void UpdateBasketOfferAccepted(bool accepted, int offerId){
          using(SqlConnection connection= new SqlConnection(connString)){
            using(SqlCommand cmd = connection.CreateCommand()){
                connection.Open();
                cmd.CommandType= CommandType.StoredProcedure;
                cmd.CommandText="UpdateOfferAccepted";
                cmd.Parameters.AddWithValue("@OfferId", offerId);
                cmd.Parameters.AddWithValue("@Accepted", accepted);
                cmd.ExecuteNonQuery();
            }
        }
    }
    public BasketOffer GetBasketOfferById(int id){
        BasketOffer basketOffer = new BasketOffer();
        using(SqlConnection connection = new SqlConnection(connString)){
            using(SqlCommand cmd = connection.CreateCommand())
            {
                connection.Open();
                cmd.CommandType= CommandType.StoredProcedure;
                cmd.CommandText="GetBasketOfferById";
                cmd.Parameters.AddWithValue("@OfferId", id);
                using(var reader= cmd.ExecuteReader()){
                    
                    while(reader.Read()){
                        if(basketOffer?.OfferId==0){
                            basketOffer.OfferId=(int)reader["OfferId"];
                                basketOffer.BasketOffered=(int)reader["BasketId"];
                                basketOffer.BasketOfferedOn=(int)reader["BasketOfferedOn"];
                                basketOffer.BasketOfferedOnName=reader["BasketOfferedOnName"].ToString();
                                basketOffer.BasketOfferedOnTFuel=(decimal)reader["BasketOfferedOnTfuel"];
                                basketOffer.BasketOfferedOnNFTs= Convert.IsDBNull(reader["BasketNftId"])?new List<BasketNFT>():
                                new List<BasketNFT>(){
                                    new BasketNFT(){
                                        BasketId=(int)reader["BasketOfferedOn"],
                                        BasketNFTId=(int)reader["BasketNftId"],
                                        Name=reader["Name"].ToString(),
                                        NFTId=(int)reader["NFTId"],
                                        TokenId=(int)reader["TokenId"]
                                    }
                                }; 
                        }else if(!Convert.IsDBNull(reader["BasketNftId"])){
                            basketOffer?.BasketOfferedOnNFTs?.Add(new BasketNFT(){
                                        BasketId=(int)reader["BasketOfferedOn"],
                                        BasketNFTId=(int)reader["BasketNftId"],
                                        Name=reader["Name"].ToString(),
                                        NFTId=(int)reader["NFTId"],
                                        TokenId=(int)reader["TokenId"]
                                    });
                        }
                    }
                }

            }
        }
        return basketOffer??(new BasketOffer());
    }

}