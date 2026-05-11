namespace LoanManagement.Entities.Enums;

public enum LoanType
{
    Personal = 0, //İhtiyaç Kredisi
    Education = 1,  //Eğitim Kredisi
    Vehicle = 2     //Taşıt Kredisi
}

public enum LoanStatus
{
    Active = 1, //Aktif (ödemeleri devam ediyor.)
    Closed = 2  //Kapatıldı (Tüm borçlar bitti.)
}

public enum InstallmentStatus
{
    Unpaid = 1, //Ödenmedi
    Paid = 2,   //Ödendi
    Overdue = 3, //Gecikmiş(Vadesi geçti ama ödenmedi)
}