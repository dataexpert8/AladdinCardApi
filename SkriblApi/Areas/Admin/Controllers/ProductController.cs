using BasketApi.Areas.Admin.ViewModels;
using BasketApi.ViewModels;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using WebApplication1.Areas.Admin.ViewModels;
using WebApplication1.BindingModels;
using System.Data.Entity;

namespace BasketApi.Areas.SubAdmin.Controllers
{
    //[BasketApi.Authorize("SubAdmin", "SuperAdmin", "ApplicationAdmin", "User", "Guest")]
    [RoutePrefix("api")]
    public class ProductController : ApiController
    {

        [HttpGet]
        [Route("GetOfferByOfferId")]
        public async Task<IHttpActionResult> GetOfferByOfferId(int Offer_Id, int? User_Id = 0)
        {
            try
            {
                using (SkriblContext ctx = new SkriblContext())
                {

                    var offer = ctx.Products.Include(x => x.Store).FirstOrDefault(x => x.Id == Offer_Id && !x.IsDeleted);
                    if (offer != null)
                    {
                        if (User_Id.HasValue && User_Id != 0)
                            offer.IsFavourite = ctx.Favourites.Any(x => x.User_ID == User_Id && x.Product_Id == offer.Id && x.IsFavourite);
                        CustomResponse<Product> response = new CustomResponse<Product>
                        {
                            Message = Global.ResponseMessages.Success,
                            StatusCode = (int)HttpStatusCode.OK,
                            Result = ctx.Products.FirstOrDefault(x => x.Id == Offer_Id)
                        };
                        return Ok(response);
                    }
                    else
                    {
                        return Content(HttpStatusCode.OK, new CustomResponse<Error>
                        {
                            Message = "NotFound",
                            StatusCode = (int)HttpStatusCode.NotFound,
                            Result = new Error { ErrorMessage = "Invalid Offer Id" }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }

        //[HttpGet]
        //[Route("GetProductCount")]
        //public async Task<IHttpActionResult> GetProductCount()
        //{
        //    try
        //    {
        //        using (SkriblContext ctx = new SkriblContext())
        //        {
        //            ProductCountViewModel model = new ProductCountViewModel { TotalProducts = ctx.Products.Count(x => x.IsDeleted == false) };
        //            CustomResponse<ProductCountViewModel> response = new CustomResponse<ProductCountViewModel>
        //            {
        //                Message = Global.ResponseMessages.Success,
        //                StatusCode = (int)HttpStatusCode.OK,
        //                Result = model
        //            };
        //            return Ok(response);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(Utility.LogError(ex));
        //    }
        //}

        [HttpGet]
        [Route("SearchOffers")]
        public async Task<IHttpActionResult> GetAllProducts(string SearchString, string Location, int? User_Id = 0, int? Page = 0, int? Items = 6, int? Category_Id = 0, double? Distance = 0, double? Latitude = 0, double? Longitude = 0)
        {
            try
            {
                using (SkriblContext ctx = new SkriblContext())
                {

                    var query = "SELECT Products.* FROM Products join Stores ON Products.Store_Id=Stores.Id Where ";

                    if (string.IsNullOrEmpty(SearchString))
                        query += " Products.Name LIKE '%" + SearchString + "%' AND ";
                    if (Category_Id != 0)
                        query += " Products.Category_Id='" + Category_Id.Value + "' AND ";
                    if (string.IsNullOrEmpty(Location))
                        query += " Stores.Address LIKE '" + Location + "' AND ";


                    query += " Products.IsDeleted=0  ORDER BY Products.Id desc OFFSET " + Page.Value * Items.Value + " ROWS FETCH NEXT " + Items.Value + " ROWS ONLY ";


                    var Offers = ctx.Database.SqlQuery<Product>(query).ToList();

                    if (Offers != null)
                    {
                        if (User_Id.HasValue && User_Id != 0)
                        {
                            foreach (var offer in Offers)
                            {
                                offer.IsFavourite = ctx.Favourites.Any(x => x.User_ID == User_Id && x.Product_Id == offer.Id && !x.IsFavourite);
                            }
                        }

                        if (Distance != 0)
                        {


                        }

                        foreach (var item in Offers)
                        {
                            item.Store = ctx.Stores.FirstOrDefault(x => x.Id == item.Store_Id);
                        }

                    }


                    return Ok(new CustomResponse<ProductsViewModel>
                    {
                        Message = Global.ResponseMessages.Success,
                        StatusCode = (int)HttpStatusCode.OK,
                        Result = new ProductsViewModel
                        {
                            Count = ctx.Products.Count(x => x.IsDeleted == false),
                            Products = Offers
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }


        [HttpPost]
        [Route("MarkOfferAsFavourite")]
        public async Task<IHttpActionResult> MarkOfferAsFavourite(FavouriteBindingModel model)
        {
            try
            {
                using (SkriblContext ctx = new SkriblContext())
                {
                    Favourite favourite = new Favourite();
                    favourite = ctx.Favourites.FirstOrDefault(x => x.User_ID == model.User_Id && x.Product_Id == model.Offer_Id);
                    if (favourite != null)
                    {
                        favourite.IsFavourite = model.IsFavourite;
                    }
                    else
                    {
                        favourite = ctx.Favourites.Add(new Favourite
                        {
                            IsDeleted = false,
                            IsFavourite = true,
                            Product_Id = model.Offer_Id,
                            User_ID = model.User_Id
                        });
                    }
                    ctx.SaveChanges();


                    CustomResponse<Favourite> response = new CustomResponse<Favourite>
                    {
                        Message = Global.ResponseMessages.Success,
                        StatusCode = (int)HttpStatusCode.OK,
                        Result = favourite
                    };
                    return Ok(response);


                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }

        [HttpGet]
        [Route("GetMyFavouriteOffers")]
        public async Task<IHttpActionResult> GetMyFavouriteOffers(int User_Id, int? Page = 0, int? Items = 10)
        {
            try
            {
                using (SkriblContext ctx = new SkriblContext())
                {
                    FavouritesViewModel returnModel = new FavouritesViewModel();

                    returnModel.Favourites = ctx.Favourites.Where(x => x.User_ID == User_Id && x.IsFavourite && !x.IsDeleted).Include(x => x.Product).OrderByDescending(x => x.Id).Skip(Page.Value * Items.Value).Take(Items.Value).ToList();
                    returnModel.TotalRecords = ctx.Favourites.Count(x => x.User_ID == User_Id && x.IsFavourite && !x.IsDeleted);

                    CustomResponse<FavouritesViewModel> response = new CustomResponse<FavouritesViewModel>
                    {
                        Message = Global.ResponseMessages.Success,
                        StatusCode = (int)HttpStatusCode.OK,
                        Result = returnModel
                    };
                    return Ok(response);


                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }


        [HttpGet]
        [Route("GetLatestOffers")]
        public async Task<IHttpActionResult> GetLatestOffers(int User_Id, int? Page = 0, int? Items = 10)
        {
            try
            {
                using (SkriblContext ctx = new SkriblContext())
                {
                    ProductsViewModel returnModel = new ProductsViewModel();

                    var products = ctx.Products.Where(x => !x.IsDeleted).ToList();
                    if (products != null)
                    {
                        //returnModel.Products = products.Where(x => x.CreatedDate.Date == DateTime.Now.Date).OrderByDescending(x => x.Id).Skip(Page.Value * Items.Value).Take(Items.Value).ToList();
                        returnModel.Products = products.Where(x => !x.IsDeleted).OrderByDescending(x => x.Id).Take(Items.Value).ToList();

                        foreach (var item in returnModel.Products)
                        {
                            item.Store = ctx.Stores.FirstOrDefault(x => x.Id == item.Store_Id);
                            item.IsFavourite = ctx.Favourites.Any(x => x.User_ID == User_Id && x.Product_Id == item.Id && !x.IsFavourite);
                        }

                        returnModel.Count = products.Count(x => x.CreatedDate.Date == DateTime.Now.Date);

                    }
                    CustomResponse<ProductsViewModel> response = new CustomResponse<ProductsViewModel>
                    {
                        Message = Global.ResponseMessages.Success,
                        StatusCode = (int)HttpStatusCode.OK,
                        Result = returnModel
                    };
                    return Ok(response);


                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }

        [HttpGet]
        [Route("GetAllOffers")]
        public async Task<IHttpActionResult> GetAllOffers()
        {
            try
            {
                using (SkriblContext ctx = new SkriblContext())
                {
                    ProductsViewModel returnModel = new ProductsViewModel();

                    returnModel.Products = ctx.Products.Where(x => !x.IsDeleted).ToList();
                   
                    CustomResponse<ProductsViewModel> response = new CustomResponse<ProductsViewModel>
                    {
                        Message = Global.ResponseMessages.Success,
                        StatusCode = (int)HttpStatusCode.OK,
                        Result = returnModel
                    };
                    return Ok(response);


                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }


        //[HttpGet]
        //[Route("GetPopularProducts")]
        //public async Task<IHttpActionResult> GetPopularProducts(int Count)
        //{
        //    try
        //    {
        //        using (SkriblContext ctx = new SkriblContext())
        //        {
        //            return Ok(new CustomResponse<ProductsViewModel>
        //            {
        //                Message = Global.ResponseMessages.Success,
        //                StatusCode = (int)HttpStatusCode.OK,
        //                Result = new ProductsViewModel { Products = ctx.Products.Where(x => x.IsDeleted == false).OrderByDescending(x => x.OrderedCount).Take(Count).ToList() }
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(Utility.LogError(ex));
        //    }
        //}
    }
}
