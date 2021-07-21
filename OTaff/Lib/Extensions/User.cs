using MongoDB.Driver;
using ServerLib;
using SharedLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OTaff.Lib.Extensions
{
    public static class UserExtensions
    {
        public static User GetUser(this Jwt jwt) => Db.UsersCollection.Find(x => x.Id == jwt.UserId).FirstOrDefault();
        public static async Task<User> GetUserAsync(this Jwt jwt) => await Db.UsersCollection.Find(x => x.Id == jwt.UserId).FirstOrDefaultAsync();

        public static bool Verify(this Jwt jwt)
        {
            try
            {
                var dbt = Db.JwtsCollection.Find(x => x.Id == jwt.Id /*&& jwt.Email == x.Email && jwt.Username == x.Username && jwt.Issued == x.Issued*/).FirstOrDefault();
                if (dbt is null) return false;
                var good =  HashingManager.Verify(jwt.Token, dbt.Token);
                if (good) Db.JwtsCollection.UpdateOneAsync(x => x.Id == jwt.Id, new UpdateDefinitionBuilder<Jwt>()
                    .Set(x=>x.LastUsed, DateTime.UtcNow));
                return good;
            }
            catch
            {
                return false;
            }
        } 


    }
}
