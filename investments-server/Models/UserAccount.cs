using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentsServer.Models;

[Table("Users")]
public class UserAccount
{
    [Key]
    [Required]
    [MaxLength(100)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } = 500m; 
    
    public List<ActiveInvestment> ActiveInvestments { get; set; } = new();
}