namespace LoanManagement.Entities.Enums;

public enum LoanType
{
    Personal = 1, //İhtiyaç Kredisi
    Education=2,  //Eğitim Kredisi
    Vehicle=3     //Taşıt Kredisi
}

public enum LoanStatus
{
    Active=1, //Aktif (ödemeleri devam ediyor.)
    Closed=2  //Kapatıldı (Tüm borçlar bitti.)
}

public enum InstallmentStatus
{
    Unpaid=1, //Ödenmedi
    Paid=2,   //Ödendi
    Overdue=3, //Gecikmiş(Vadesi geçti ama ödenmedi)
}