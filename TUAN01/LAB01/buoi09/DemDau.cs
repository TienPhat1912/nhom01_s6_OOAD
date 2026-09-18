using System;
class DemDau
{
    const int MAX_N = 500;
    const double EPS = 1e-9;   // ngưỡng so sánh số thực với 0

    public static void nhapDaySo(ref double[] day)
    {
        int n;
        do
        {
            Console.Write("Moi ban nhap so luong phan tu: ");
            n = int.Parse(Console.ReadLine() ?? "0");
        }
        while (n < 0 || n > MAX_N);

        day = new double[n];
        for (int i = 0; i <= day.Length - 1; i++)
        {
                Console.Write("Phan tu {0}: ", i);
                day[i] = double.Parse(Console.ReadLine() ?? "0");
        }
    }

    // Đếm số âm / số không / số dương trong 1 lần duyệt (O(n))
    public static void demDau(double[] day, ref int soAm, ref int soKhong, ref int soDuong)
    {
        soAm = soKhong = soDuong = 0;
        foreach (double x in day)
        {
            if (Math.Abs(x) <= EPS)   // KHÔNG so sánh x == 0 với kiểu thực
                soKhong++;
            else if (x < 0)
                soAm++;
            else
                soDuong++;
        }
    }

    public static void Main(string[] args)
    {
        double[] a = null;
        int am = 0, khong = 0, duong = 0;

        nhapDaySo(ref a);
        demDau(a, ref am, ref khong, ref duong);

        Console.WriteLine("Day so co {0} so am, {1} so khong va {2} so duong.", am, khong, duong);
    }
}