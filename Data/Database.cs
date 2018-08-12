using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AcmeGames.Models;
using Newtonsoft.Json;
using System.Linq;

namespace AcmeGames.Data
{
	public class Database
	{
        private static readonly Random          locRandom       = new Random();

		private static IEnumerable<Game>		locGames		= new List<Game>();
		private static IEnumerable<GameKey>		locKeys			= new List<GameKey>();
		private static IEnumerable<Ownership>	locOwnership	= new List<Ownership>();
		private static IEnumerable<User>		locUsers		= new List<User>();

        private static Database _instance = new Database();

		private Database()
		{
			locGames		= JsonConvert.DeserializeObject<IEnumerable<Game>>(File.ReadAllText(@"Data\games.json"));
			locKeys			= JsonConvert.DeserializeObject<IEnumerable<GameKey>>(File.ReadAllText(@"Data\keys.json"));
			locUsers		= JsonConvert.DeserializeObject<IEnumerable<User>>(File.ReadAllText(@"Data\users.json"));
			locOwnership	= JsonConvert.DeserializeObject<IEnumerable<Ownership>>(File.ReadAllText(@"Data\ownership.json"));
		}

        public static Database GetInstance()
        {
            if(_instance == null)
            {
                return new Database();
            }
            return _instance;
        }

	    // NOTE: This accessor function must be used to access the data.
	    private Task<IEnumerable<T>>
	    PrivGetData<T>(
	        IEnumerable<T>  aDataSource)
	    {
	        var delay = locRandom.Next(150, 1000);
            Thread.Sleep(TimeSpan.FromMilliseconds(delay));

	        return Task.FromResult(aDataSource);
	    }

        public async Task<IEnumerable<User>> GetUsers(Func<User, bool> query = null)
        {
            if(query == null)
            {
                return await PrivGetData(locUsers);
            }
            else
            {
                return await PrivGetData(locUsers.Where(query));
            }
        }

        public async Task<IEnumerable<Game>> GetUserOwnedGames(string userAccountId = null)
        {
            IEnumerable<Ownership> ownerShips;
            if (userAccountId == null)
            {
                ownerShips = await PrivGetData(locOwnership);
            }
            else
            {
                ownerShips = await PrivGetData(locOwnership.Where(os=>os.UserAccountId == userAccountId));
            }
            return await PrivGetData(locGames).ContinueWith(t => t.Result.Where(g => ownerShips.Select(os => os.GameId).ToList().Contains(g.GameId)));
        }

        public async Task<IEnumerable<dynamic>> GetUserOwnershipsInfo(string userAccountId = null)
        {
            IEnumerable<Ownership> ownerShips;
            if (userAccountId == null)
            {
                ownerShips = await PrivGetData(locOwnership);
            }
            else
            {
                ownerShips = await PrivGetData(locOwnership.Where(os => os.UserAccountId == userAccountId));
            }
            var games = await PrivGetData(locGames).ContinueWith(t => t.Result.Where(g => ownerShips.Select(os => os.GameId).ToList().Contains(g.GameId)));
            var query = from os in ownerShips
                        join game in games on os.GameId equals game.GameId
                        select new
                        {
                            os.OwnershipId,
                            os.GameId,
                            os.UserAccountId,
                            os.RegisteredDate,
                            os.State,
                            game.Name,
                            game.AgeRestriction,
                            game.Thumbnail
                        };
            return query.ToList();
        }

        public async Task<IEnumerable<dynamic>> GetAllGameInfoByUser(string userAccountId)
        {
            var games = await PrivGetData(locGames);
            IEnumerable<Ownership> ownerShips;
            ownerShips = await PrivGetData(locOwnership.Where(os => os.UserAccountId == userAccountId));
            
            var query = from g in games
                        join s in ownerShips on g.GameId equals s.GameId into gameownership
                        from go in gameownership.DefaultIfEmpty(new Ownership() { State = OwnershipState.Initial})
                        select new
                        {
                            go.OwnershipId,
                            g.GameId,
                            userAccountId,
                            go.RegisteredDate,
                            go.State,
                            g.Name,
                            g.AgeRestriction,
                            g.Thumbnail
                        };
            return query.ToList();
        }

        public async Task<dynamic> GrantUserGameAsync(string userAccountId, uint gameId)
        {
            IEnumerable<Ownership> ownerShips;
            ownerShips = await PrivGetData(locOwnership.Where(os => os.UserAccountId == userAccountId && os.GameId == gameId));
            var games = await PrivGetData(locGames);
            Ownership targetOwnership = ownerShips.FirstOrDefault();
            if(targetOwnership!= null)
            {
                targetOwnership.State = OwnershipState.Owned;
            }
            else
            {
                //Add ownership
                uint latestId = locOwnership.Max(os => os.OwnershipId);
                targetOwnership = new Ownership()
                {
                    GameId = gameId,
                    OwnershipId = latestId + 1,
                    State = OwnershipState.Owned,
                    UserAccountId = userAccountId,
                    RegisteredDate = DateTime.Now.ToString("yyyy-MM-dd")
                };
                locOwnership = locOwnership.Append(targetOwnership);
            }

            var query = from g in games
                        where g.GameId == gameId
                        select new
                        {
                            targetOwnership.OwnershipId,
                            g.GameId,
                            userAccountId,
                            targetOwnership.RegisteredDate,
                            targetOwnership.State,
                            g.Name,
                            g.AgeRestriction,
                            g.Thumbnail
                        };
            return query.ToList().FirstOrDefault();
        }

        public async Task<IEnumerable<GameKey>> GetGameKeys(Func<GameKey, bool> query = null)
        {
            if (query == null)
            {
                return await PrivGetData(locKeys);
            }
            else
            {
                return await PrivGetData(locKeys.Where(query));
            }
        }

        public async Task<IEnumerable<Ownership>> GetOwnship(Func<Ownership, bool> query = null)
        {
            if (query == null)
            {
                return await PrivGetData(locOwnership);
            }
            else
            {
                return await PrivGetData(locOwnership.Where(query));
            }
        }

        public async Task<IEnumerable<Game>> RedeemGameKey(string key, string userAccountId)
        {
            //Redeem Key
            var find = locKeys.Where(k => k.Key.Equals(key, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
            find.IsRedeemed = true;
            //Add ownership
            uint latestId = locOwnership.Max(os => os.OwnershipId);
            locOwnership = locOwnership.Append(new Ownership()
            {
                GameId = find.GameId,
                OwnershipId = latestId + 1,
                State = 0,
                UserAccountId = userAccountId,
                RegisteredDate = DateTime.Now.ToString("yyyy-MM-dd")
            });
            //Return new owned game
            return await PrivGetData(locGames.Where(g=>g.GameId == find.GameId));
        }

        public User UpdateProfile(User user)
        {
            //Update profile
            var find = locUsers.Where(u => u.UserAccountId.Equals(user.UserAccountId)).FirstOrDefault();
            if(find != null)
            {
                find.FirstName = user.FirstName;
                find.LastName = user.LastName;
                find.EmailAddress = user.EmailAddress;
                find.Password = user.Password;
            }
            return find;
        }
    }
}
