/*
* CHƯƠNG TRÌNH TÌM SỐ NGÀY CỦA THÁNG (DÙNG RẺ NHÁNH)
* Tác giả  : Nguyễn Khải Hoàn Ca
* Ngày viết: 9/9/2026
*
* Phát biểu đề bài:
* Nhập vào tháng M và năm Y.
* Xác định tháng M của năm Y có bao nhiêu ngày.
*
* Ý tưởng:
* - Các tháng 1, 3, 5, 7, 8, 10, 12 có 31 ngày.
* - Các tháng 4, 6, 9, 11 có 30 ngày.
* - Tháng 2 có 28 hoặc 29 ngày tùy năm nhuận.
*
* Mã giả:
* 1. Nhập M, Y
* 2. Nếu M thuộc nhóm tháng 31 ngày thì in 31
* 3. Nếu M = 2:
*    - Nếu Y là năm nhuận thì in 29
*    - Ngược lại in 28
* 4. Nếu M thuộc nhóm tháng 30 ngày thì in 30
* 5. Ngược lại báo tháng không hợp lệ
*/

using System;

class Program
{
    static void Main()
    {
        Console.Write("Enter month: ");
        int M = int.Parse(Console.ReadLine());

        Console.Write("Enter year: ");
        int Y = int.Parse(Console.ReadLine());

        if (M == 1 || M == 3 || M == 5 || M == 7 ||
            M == 8 || M == 10 || M == 12)
        {
            Console.WriteLine($"Month {M} in {Y} has 31 days");
        }
        else if (M == 2)
        {
            if (Y % 400 == 0 || (Y % 4 == 0 && Y % 100 != 0))
            {
                Console.WriteLine($"Month {M} in {Y} has 29 days");
            }
            else
            {
                Console.WriteLine($"Month {M} in {Y} has 28 days");
            }
        }
        else if (M == 4 || M == 6 || M == 9 || M == 11)
        {
            Console.WriteLine($"Month {M} in {Y} has 30 days");
        }
        else
        {
            Console.WriteLine("Invalid month");
        }
    }
}