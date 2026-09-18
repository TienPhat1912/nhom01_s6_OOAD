/*
 * CHƯƠNG TRÌNH ĐỔI SANG GIỜ PHÚT GIÂY
 * Tác giả  : Nguyễn Khải Hoàn Ca
 * Ngày viết: 7/9/2026
 *
 * Phát biểu đề bài: Viết chương trình nhập số giây và chuyển thành giờ phút giây 
 * 
 * Ý tưởng: 
 * Số giờ được tính bằng:
 *   h = s / 3600
 *
 * Số phút còn lại được tính bằng:
 *   m = (s % 3600) / 60
 *
 * Số giây còn lại được tính bằng:
 *   sec = s % 60
 * 
 * Mã giả: 
 * Bắt đầu:
 * 1 Nhập s
 * 2 h = s / 3600
 * 3 m = (s % 3600) / 60
 * 4 sec = s % 60
 * 5 In h, m, sec
 * Kết thúc
 */

Console.Write("Enter seconds: ");
int s = int.Parse(Console.ReadLine());

int h = s / 3600;
int m = (s % 3600) / 60;
int sec = s % 60;

Console.WriteLine($"{s}s is: {h}:{m}:{sec}");