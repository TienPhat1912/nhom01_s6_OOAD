/*
 * CHƯƠNG TRÌNH TÍNH TỔNG PHÂN SỐ THEO SAI SỐ
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập a và sai số epsilon, tính tổng các phân số 1/(a+i) cho đến khi số hạng nhỏ hơn epsilon.
 *
 * Ý tưởng: Dùng vòng lặp while để cộng từng số hạng khi số hạng còn lớn hơn hoặc bằng sai số cho phép.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập a và epsilon
 * 2. Đặt S = 0, i = 0
 * 3. Khi 1/(a+i) >= epsilon, cộng số hạng vào S và tăng i
 * 4. In S
 * Kết thúc
 */

using System;

namespace NMLT.Buoi05
{
    class TongPhanSoSaiSo
    {
        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap so a: ");
            double a = double.Parse(Console.ReadLine());
            Console.Write("Moi ban nhap sai so epsilon (<=0.1): ");
            double epsilon = double.Parse(Console.ReadLine());

            double s = 0;
            int i = 0;
            double term = 1.0 / (a + i);

            while (term >= epsilon)
            {
                s += term;
                i++;
                term = 1.0 / (a + i);
            }

            Console.WriteLine("Gia tri cua bieu thuc S(a={0}, epsilon={1}) = {2:0.000}.", a, epsilon, s);
            Console.Read();
        }
    }
}
