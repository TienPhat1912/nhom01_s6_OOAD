/*
* CHƯƠNG TRÌNH TÌM NGÀY SAU
* Tác giả  : Nguyễn Khải Hoàn Ca
* Ngày viết: 10/9/2026
*
* Phát biểu đề bài:
* Nhập vào ngày, tháng, năm.
* Xác định ngày tiếp theo của ngày đã nhập.
*
* Ý tưởng:
* Xác định số ngày của tháng hiện tại.
* - Các tháng 1, 3, 5, 7, 8, 10, 12 có 31 ngày.
* - Các tháng 4, 6, 9, 11 có 30 ngày.
* - Tháng 2 có 28 hoặc 29 ngày tùy theo năm nhuận.
*
* Sau đó tăng ngày lên 1.
* Nếu ngày vượt quá số ngày của tháng:
* - Đặt ngày = 1
* - Tăng tháng lên 1
*
* Nếu tháng vượt quá 12:
* - Đặt tháng = 1
* - Tăng năm lên 1
*
* Mã giả:
* 1. Nhập day, month, year
* 2. Xác định daysInMonth
* 3. Nếu month thuộc nhóm tháng 31 ngày:
*    - daysInMonth = 31
* 4. Ngược lại nếu month thuộc nhóm tháng 30 ngày:
*    - daysInMonth = 30
* 5. Ngược lại:
*    - Nếu year là năm nhuận thì daysInMonth = 29
*    - Ngược lại daysInMonth = 28
* 6. Tăng day lên 1
* 7. Nếu day > daysInMonth:
*    - day = 1
*    - month = month + 1
* 8. Nếu month > 12:
*    - month = 1
*    - year = year + 1
* 9. In day/month/year
*/

using System;

class Program
{
    static void Main()
    {
        Console.Write("Enter day, month, year: ");
        string[] input = Console.ReadLine().Split();

        int day = int.Parse(input[0]);
        int month = int.Parse(input[1]);
        int year = int.Parse(input[2]);

        int daysInMonth;

        // Determine number of days in current month
        if (month == 1 || month == 3 || month == 5 ||
            month == 7 || month == 8 || month == 10 || month == 12)
        {
            daysInMonth = 31;
        }
        else if (month == 4 || month == 6 || month == 9 || month == 11)
        {
            daysInMonth = 30;
        }
        else
        {
            // February
            if (year % 400 == 0 || (year % 4 == 0 && year % 100 != 0))
            {
                daysInMonth = 29;
            }
            else
            {
                daysInMonth = 28;
            }
        }
        day++;
        if (day > daysInMonth)
        {
            day = 1;
            month++;

            if (month > 12)
            {
                month = 1;
                year++;
            }
        }
        Console.WriteLine($"The next day is {day}/{month}/{year}.");
    }
}