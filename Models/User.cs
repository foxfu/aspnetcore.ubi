using System;

namespace AcmeGames.Models
{
	/// <summary>
	/// Dev updated
	/// </summary>
	public class User
	{
		/// <summary>
		/// Master change 2
		/// </summary>
	    public string	UserAccountId { get; set; }
		/// <summary>
		/// First name
		/// </summary>
	    public string	FirstName { get; set; }
	    public string	LastName { get; set; }
	    public string   FullName => $"{FirstName} {LastName}";
	    public DateTime	DateOfBirth { get; set; }
	    public string	EmailAddress { get; set; }
		/// <summary>
		/// master added 1
		/// </summary>
	    public string	Password { get; set; }
		/// <summary>
		/// master added 2
		/// </summary>
	    public bool		IsAdmin { get; set; }
	}
}
