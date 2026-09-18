using System;
class TimKiem
{
    const int MAX_N = 500;
    const double EPS = 1e-9;

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
        for (int i = 0; i <= n - 1; i++)
        {
            Console.Write("Phan tu {0}: ", i);
            day[i] = double.Parse(Console.ReadLine() ?? "0");
        }
    }

    public static void inMangTraiSangPhai(double[] day)
    {
        Console.Write("Day so co {0} phan tu:", day.Length);
        for (int i = 0; i <= day.Length - 1; i++)
            Console.Write(" {0}", day[i]);
        Console.WriteLine();
    }

    // So sánh hai số thực bằng ngưỡng sai số tuyệt đối
    private static bool bangNhau(double a, double b)
    {
        return Math.Abs(a - b) <= EPS;
    }

    // Một lần duyệt: lấy đồng thời vị trí đầu, vị trí cuối và số lần xuất hiện
    public static void timKiem(double[] day, double x,
                                ref int vtDau, ref int vtCuoi, ref int soLan)
    {
        vtDau = vtCuoi = -1;
        soLan = 0;
        for (int i = 0; i <= day.Length - 1; i++)
        {
            if (bangNhau(day[i], x))
            {
                if (vtDau == -1)
                    vtDau = i;
                vtCuoi = i;
                soLan++;
            }
        }
    }

    public static void Main(string[] args)
    {
        double[] a = null;
        double x;
        int vtDau = -1, vtCuoi = -1, soLan = 0;

        nhapDaySo(ref a);
        inMangTraiSangPhai(a);

        Console.WriteLine("* Tim kiem");
        Console.Write("Moi ban nhap gia tri x can tim: ");
        x = double.Parse(Console.ReadLine() ?? "0");

        timKiem(a, x, ref vtDau, ref vtCuoi, ref soLan);

        if (soLan == 0)
            Console.WriteLine("Gia tri {0} khong ton tai trong day so.", x);
        else
            Console.WriteLine("Gia tri {0} xuat hien {1} lan o vi tri dau tien {2} va cuoi cung {3}.", x, soLan, vtDau, vtCuoi);
    }
}