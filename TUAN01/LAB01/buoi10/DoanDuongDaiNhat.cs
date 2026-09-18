// Buổi 10 - Bài tập cơ bản - Bài 5: Tìm đoạn dương dài nhất
// File: DoanDuongDaiNhat.cs
// Kỹ thuật: duyệt 1 lần, dùng biến trạng thái curFrom đánh dấu đầu đoạn dương
//           hiện tại -> O(n), KHÔNG cần lồng 2 vòng như bài mẫu của sách.
using System;
using System.Globalization;

namespace NMLT.Buoi10
{
    class DoanDuongDaiNhat
    {
        const int MAX_N = 500;
        const double EPS = 1e-9;

        public static void nhapDaySo(ref double[] day)
        {
            int n;
            do
            {
                Console.Write("Moi ban nhap so luong phan tu: ");
            } while (!int.TryParse(Console.ReadLine(), out n) || n <= 0 || n > MAX_N);

            day = new double[n];
            for (int i = 0; i <= n - 1; i++)
            {
                do
                {
                    Console.Write("Phan tu {0}: ", i);
                } while (!double.TryParse(Console.ReadLine(), NumberStyles.Float,
                                          CultureInfo.InvariantCulture, out day[i]));
            }
        }

        public static void inMangTraiSangPhai(double[] day)
        {
            Console.Write("Day so co {0} phan tu:", day.Length);
            for (int i = 0; i <= day.Length - 1; i++)
                Console.Write(" {0}", day[i].ToString("G", CultureInfo.InvariantCulture));
            Console.WriteLine();
        }

        public static void inDayCon(double[] day, int from, int to)
        {
            for (int i = from; i <= to; i++)
                Console.Write(" {0}", day[i].ToString("G", CultureInfo.InvariantCulture));
        }

        private static bool laDuong(double x)
        {
            return x > EPS;   // số 0 (và nhiễu quanh 0) không được coi là dương
        }

        // Trả về true nếu tồn tại ít nhất một phần tử dương
        public static bool timDoanDuongDaiNhat(double[] day, ref int fromMax, ref int toMax)
        {
            fromMax = -1; toMax = -1;
            int doDaiMax = 0;
            int curFrom = -1;   // -1: đang không ở trong đoạn dương nào

            for (int i = 0; i <= day.Length - 1; i++)
            {
                if (laDuong(day[i]))
                {
                    if (curFrom == -1)      // mở đoạn mới
                        curFrom = i;

                    if (i - curFrom + 1 > doDaiMax)   // cập nhật ngay, không cần chờ đóng đoạn
                    {
                        doDaiMax = i - curFrom + 1;
                        fromMax = curFrom;
                        toMax = i;
                    }
                }
                else
                {
                    curFrom = -1;           // đóng đoạn
                }
            }

            return fromMax != -1;
        }

        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            double[] a = null;
            int i = 0, j = 0;

            nhapDaySo(ref a);
            inMangTraiSangPhai(a);

            if (!timDoanDuongDaiNhat(a, ref i, ref j))
            {
                Console.WriteLine("Day so khong co phan tu duong.");
            }
            else
            {
                Console.Write("Doan duong [{0}, {1}] dai nhat:", i, j);
                inDayCon(a, i, j);
                Console.WriteLine();
            }
        }
    }
}
