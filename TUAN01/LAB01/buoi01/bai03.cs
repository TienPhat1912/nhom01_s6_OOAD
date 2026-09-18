/*
 * CHƯƠNG TRÌNH ĐỔI SANG GIÂY
 * Tác giả  : Nguyễn Khải Hoàn Ca
 * Ngày viết: 7/9/2026
 *
 * Phát biểu đề bài: Viết chương trình nhập giờ phút giây và chuyển toàn bộ sang giây
 * 
 * Ý tưởng: 
 * 1 giờ = 3600 giây
 * 1 phút = 60 giây
 * Tổng số giây = giờ * 3600 + phút * 60 + giây
 * 
 * Mã giả: 
 * Bắt đầu:
 * 1. Nhập h
 * 2. Nhập m
 * 3. Nhập s
 * 4. result = h * 3600 + m * 60 + s
 * 5. In result
 * Kết thúc
 */

Console.WriteLine("Enter hour: ");
double h = double.Parse(Console.ReadLine());

Console.WriteLine("Enter minute: ");
double m = double.Parse(Console.ReadLine());

Console.WriteLine("Enter second: ");
double s = double.Parse(Console.ReadLine());

double result = h*3600 + m*60 + s;

Console.WriteLine("Sum of " + h + ":" + m + ":" + s + " is: " + result + "s");