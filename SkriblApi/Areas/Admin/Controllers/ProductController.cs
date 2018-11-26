using BasketApi.Areas.Admin.ViewModels;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using WebApplication1.Areas.Admin.ViewModels;

namespace BasketApi.Areas.SubAdmin.Controllers
{
    //[BasketApi.Authorize("SubAdmin", "SuperAdmin", "ApplicationAdmin", "User", "Guest")]
    [RoutePrefix("api")]
    public class ProductController : ApiController
    {
        
        [HttpGet]
        [Route("GetOfferByOfferId")]
        public async Task<IHttpActionResult> GetOfferByOfferId(int Offer_Id)
        {
            try
            {
                using (SkriblContext ctx = new SkriblContext())
                {
                    CustomResponse<Product> response = new CustomResponse<Product>
                    {
                        Message = Global.ResponseMessages.Success,
                        StatusCode = (int)HttpStatusCode.OK,
                        Result = ctx.Products.FirstOrDefault(x => x.Id == Offer_Id)
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
        public async Task<IHttpActionResult> GetAllProducts(string SearchString,string Location,int? Page=0, int? Items=6,int? Category_Id=0)
        {
            try
            {
                using (SkriblContext ctx = new SkriblContext())
                {




                    var query = "SELECT Products.* FROM Products join Stores ON Products.Store_Id=Stores.Id Where ";

                    if (string.IsNullOrEmpty(SearchString))
                        query += " Products.Name LIKE '%"+SearchString+"%' AND ";
                    if (Category_Id != 0)
                        query += " Products.Category_Id='"+Category_Id.Value+"' AND ";
                    if(string.IsNullOrEmpty(Location))
                        query += " Stores.Address LIKE '" + Location + "' AND ";
                    

                    query += " Products.IsDeleted=0  ORDER BY Products.Id OFFSET "+Page.Value*Items.Value+ " ROWS FETCH NEXT "+Items.Value+" ROWS ONLY ";


                    var Offers = ctx.Database.SqlQuery<Product>(query).ToList();

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
