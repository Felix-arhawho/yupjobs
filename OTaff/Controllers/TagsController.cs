using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OTaff.Lib;
using OTaff.Lib.Extensions;
using ServerLib;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Controllers
{
    [Route("api/tags")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        [HttpPost("get/{tag}")]
        public ActionResult<string> GetTags(string tag)
        {
            var tags = Db.TagMetasLocalCollection.FindAll(x => x.TagName.Contains(tag));
            return tags.ToJson();
        }
            //=> Db.TagMetaCollection.FindSync(x => x.TagName.Contains(tag), new FindOptions<DbTag, DbTag> 
            //{
            //    Sort = new SortDefinitionBuilder<DbTag>().Descending(x=>x.UseCount),
            //    Limit = 20
            //}).ToList().ToJson();

        [HttpPost("top/{count}")]
        public ActionResult<string> GetTop(short count = 5)
        {
            return Db.TagMetasLocalCollection.OrderByDescending(x => x.UseCount).Take(count).ToList().ToJson();
        }
        
    }
}