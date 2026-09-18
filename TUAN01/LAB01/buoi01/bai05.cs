/*
 * CHƯƠNG TRÌNH DIỆN TÍCH TAM GIÁC
 * Tác giả  : Nguyễn Khải Hoàn Ca
 * Ngày viết: 7/9/2026
 *
 * Phát biểu đề bài: Viết chương trình nhập cạnh a, b, c của tam giác.
 * Tính diện tích tam giác bằng công thức Heron
 * 
 * Ý tưởng: 
 * Tính nửa chu vi p = (a + b + c) / 2
 * Sau đó tính diện tích S = Sqrt((p(p-a)(p-b)(p-c)))
 * Mã giả: 
 *
 * Bắt đầu:
 * Bắt đầu
 * 1. Nhập a
 * 2. Nhập b
 * 3. Nhập c
 * 4. Tính p = (a + b + c) / 2
 * 5. Tính S = Sqrt((p * (p - a) * (p - b) * (p - c)))
 * 6. In S
 * Kết thúc
 */

Console.WriteLine("Enter length of a");
double a = double.Parse(Console.ReadLine());

Console.WriteLine("Enter length of b");
double b = double.Parse(Console.ReadLine());

Console.WriteLine("Enter length of c");
double c = double.Parse(Console.ReadLine());

double p = (a + b + c) / 2;
double S = Math.Sqrt( p*(p-a)*(p-b)*(p-c) );

Console.WriteLine($"Area of triangle:  {S:F2}");