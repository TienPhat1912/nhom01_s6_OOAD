/*
 * CHƯƠNG TRÌNH TÍNH DIỆN TÍCH VÀ CHU VI HÌNH TRÒN
 * Tác giả  : Nguyễn Khải Hoàn Ca
 * Ngày viết: 7/9/2026
 *
 * Phát biểu đề bài: Viết chương trình nhập bán kính, tính diện tích và chu vi hình tròn
 * 
 * Ý tưởng: Dùng công thức diện tích = pi * r * r, chu vi = pi * r * 2
 * 
 * Mã giả: 
 * Bắt đầu:
 * 1. Nhập r
 * 2. Tính diện tích = pi * r * r
 * 3. Tính chu vi = pi * r * 2
 * 4. In diện tích, chu vi
 * Kết thúc
 */
Console.WriteLine("Enter radius: ");

double r = double.Parse(Console.ReadLine());

double area = Math.PI * r * r;
double perimeter = Math.PI * r * 2;

Console.WriteLine("Area of circle: " + area);
Console.WriteLine("Perimeter of circle: " + perimeter);