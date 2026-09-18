using System;
class TimKiemGiaTriChan
{
    const int MAX_N = 500;

    public static void nhapDaySo(ref int[] day)
    {
        int n;
        do
        {
            Console.Write("Moi ban nhap so luong phan tu: ");
            n = int.Parse(Console.ReadLine() ?? "0");
        }
        while (n < 0 || n > MAX_N);

        day = new int[n];
        for (int i = 0; i <= day.Length - 1; i++)
        {
            Console.Write("Phan tu {0}: ", i);
            day[i] = int.Parse(Console.ReadLine() ?? "0");
        }
    }

    // Duyệt từ trái sang phải, dừng ngay khi gặp phần tử chẵn đầu tiên
    // Trả về -1 nếu không tồn tại
    public static int viTriChanDauTien(int[] day)
    {
        for (int i = 0; i <= day.Length - 1; i++)
            if (day[i] % 2 == 0)
                return i;
        return -1;
    }

    // Duyệt từ phải sang trái, dừng ngay khi gặp phần tử chẵn đầu tiên tính từ cuối
    public static int viTriChanCuoiCung(int[] day)
    {
        for (int i = day.Length - 1; i >= 0; i--)
            if (day[i] % 2 == 0)
                return i;
        return -1;
    }

    public static void Main(string[] args)
    {
        int[] a = null;
        nhapDaySo(ref a);

        int vtDau = viTriChanDauTien(a);
        int vtCuoi = viTriChanCuoiCung(a);

        if (vtDau == -1)   // vtDau == -1 <=> vtCuoi == -1
        {
            Console.WriteLine("Day so khong chua gia tri chan.");
        }
        else
        {
            Console.WriteLine("Gia tri chan dau tien o vi tri {0} co gia tri {1}.", vtDau, a[vtDau]);
            Console.WriteLine("Gia tri chan cuoi cung o vi tri {0} co gia tri {1}.", vtCuoi, a[vtCuoi]);
        }
    }
}