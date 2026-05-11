using System;
using System.Collections.Generic;

namespace LoanManagement.Entities.DTOs
{
    public class LoanResponseDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string CustomerFullName { get; set; } = null!;
        public string LoanTypeName { get; set; } = null!;
        public decimal Amount { get; set; }
        public int Tenor { get; set; }
        public decimal ProfitRate { get; set; }
        public DateTime StartDate { get; set; }
        public string Status { get; set; } = null!;
        public List<InstallmentDto> Installments { get; set; } = new();
    }
}