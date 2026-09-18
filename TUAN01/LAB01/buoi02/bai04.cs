/*
* CHƯƠNG TRÌNH TÌM QUÝ
* Tác giả  : Nguyễn Khải Hoàn Ca
* Ngày viết: 8/9/2026
*
* Phát biểu đề bài:
* Nhập vào một tháng M.
* Xác định tháng đó thuộc quý nào trong năm.
*
* Ý tưởng:
* Một năm được chia thành 4 quý:
* - Quý 1: tháng 1 đến tháng 3
* - Quý 2: tháng 4 đến tháng 6
* - Quý 3: tháng 7 đến tháng 9
* - Quý 4: tháng 10 đến tháng 12
* Sử dụng cấu trúc if - else if - else để xác định quý.
*
* Mã giả:
* 1. Nhập M
* 2. Nếu M < 4 thì in Quarter 1
* 3. Ngược lại nếu M < 7 thì in Quarter 2
* 4. Ngược lại nếu M < 10 thì in Quarter 3
* 5. Ngược lại in Quarter 4
*/

Console.WriteLine("Enter month: ");
int M = int.Parse(Console.ReadLine());

if (M < 4)
{
    Console.WriteLine("Month " + M + " belongs to Quarter 1");
} else if (M < 7)
{
    Console.WriteLine("Month " + M + " belongs to Quarter 2");
} else if (M < 10)
{
    Console.WriteLine("Month " + M + " belongs to Quarter 3");
} else
{
    Console.WriteLine("Month " + M + " belongs to Quarter 4");
}