namespace LoanManagement.Entities;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
    public const string AdminOrCustomer = Admin + "," + Customer;
}


public static class JwtClaimNames
{
    public const string CustomerId = "customer_id";
}
