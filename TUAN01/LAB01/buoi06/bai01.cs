/*
 * CHƯƠNG TRÌNH IN CHỮ I VÀ U
 * Tác giả  : Trần Tiến Phát
 * Ngày viết: 18/09/2026
 *
 * Phát biểu đề bài: Nhập chiều cao h, in hai chữ I và U bằng các ký tự '*'.
 *
 * Ý tưởng: Dùng vòng lặp theo từng dòng, xét vị trí để in nét của chữ I và chữ U.
 *
 * Mã giả:
 * Bắt đầu:
 * 1. Nhập h
 * 2. Với mỗi dòng từ 1 đến h, in nét chữ I tương ứng
 * 3. In hai cạnh chữ U, riêng dòng cuối in thêm đáy
 * Kết thúc
 */

using System;

namespace NMLT.Buoi06
{
    class InChuIvaU
    {
        public static void Main(string[] args)
        {
            Console.Write("Moi ban nhap chieu cao h: ");
            int h = int.Parse(Console.ReadLine());

            Console.WriteLine("Chu I va U ung voi h = {0}:", h);

            // In chữ I và U theo từng dòng
            for (int i = 1; i <= h; i++)
            {
                // In 1 dòng chữ I (nét gạch ngang trên/dưới và nét dọc ở giữa)
                if (i == 1 || i == h)
                    Console.Write("*****   ");
                else
                    Console.Write("  *     ");

                // In 1 dòng chữ U (2 cạnh bên và nét đáy)
                if (i == h)
                    Console.WriteLine("*****");
                else
                    Console.WriteLine("*   *");
            }

            Console.Read();
        }
    }
}
