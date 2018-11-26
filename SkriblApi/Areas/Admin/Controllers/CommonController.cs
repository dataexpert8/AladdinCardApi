using BasketApi;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using static BasketApi.Utility;
using System.Data.Entity;

namespace WebApplication1.Areas.Admin.Controllers
{
    [BasketApi.Authorize("SubAdmin", "SuperAdmin", "ApplicationAdmin", "User")]
    [RoutePrefix("api")]
    public class CommonController : ApiController
    {
        [HttpGet]
        [Route("GetEntityById")]
        public async Task<IHttpActionResult> GetEntityById(int EntityType, int Id)
        {
            try
            {
                using (SkriblContext ctx = new SkriblContext())
                {
                    switch (EntityType)
                    {
                        case (int)BasketEntityTypes.Product:
                            return Ok(new CustomResponse<Product> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = ctx.Products.FirstOrDefault(x => x.Id == Id && x.IsDeleted == false) });

                        case (int)BasketEntityTypes.Category:
                            return Ok(new CustomResponse<Category> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = ctx.Categories.FirstOrDefault(x => x.Id == Id && x.IsDeleted == false) });

                        case (int)BasketEntityTypes.Store:
                            return Ok(new CustomResponse<Store> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = ctx.Stores.Include(x => x.StoreDeliveryHours).FirstOrDefault(x => x.Id == Id && x.IsDeleted == false) });

                        case (int)BasketEntityTypes.Package:
                            return Ok(new CustomResponse<Package> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = ctx.Packages.FirstOrDefault(x => x.Id == Id && x.IsDeleted == false) });

                        case (int)BasketEntityTypes.Admin:
                            return Ok(new CustomResponse<DAL.Admin> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = ctx.Admins.FirstOrDefault(x => x.Id == Id && x.IsDeleted == false) });

                        case (int)BasketEntityTypes.Offer:
                            return Ok(new CustomResponse<DAL.Offer> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = ctx.Offers.FirstOrDefault(x => x.Id == Id && x.IsDeleted == false) });

                        case (int)BasketEntityTypes.Box:
                            return Ok(new CustomResponse<DAL.Box> { Message = Global.ResponseMessages.Success, StatusCode = (int)HttpStatusCode.OK, Result = ctx.Boxes.Include(x=>x.BoxVideos).FirstOrDefault(x => x.Id == Id && x.IsDeleted == false) });

                        default:
                            return Ok(new CustomResponse<Error> { Message = Global.ResponseMessages.BadRequest, StatusCode = (int)HttpStatusCode.BadRequest, Result = new Error { ErrorMessage = "Invalid entity type" } });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(Utility.LogError(ex));
            }
        }
    }
}
