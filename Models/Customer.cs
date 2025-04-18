namespace CustomerManagementSystem.Models
{
	public class Customer
	{

		public int Id { get; set; }


		public string? CompanyName { get; set; }

		public int Reference { get; set; }


		public string? Code { get; set; }


		public string? ContactName { get; set; }


		public string? Mail { get; set; }


		public int Tel { get; set; }


		public string? ServiceArea { get; set; }

		public bool IsDeleted { get; set; } = false;
	}
}
