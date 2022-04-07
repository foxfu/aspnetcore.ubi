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
	    public string	Password { get; set; }
	    public bool		IsAdmin { get; set; }
	}
}
