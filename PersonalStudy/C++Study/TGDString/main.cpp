#include "TGDString.h"
#include <iostream>

int main(int argc, char* argv[])
{
    {
        TGD::TGDString ts;
        ts.Append("hello.");
        ts.Append("1234567890");
        ts.Append("-");
        ts.Append("987654321");
        ts.Append("-");
        ts.Append("13579 - 08642");

        TGD::TGDString ts2(ts);

        std::cout << "ts = " << ts.Cstr() << std::endl;
        std::cout << "ts2 = " << ts2.Cstr() << std::endl;
        std::cout << "ts.maxSize = " << ts.MaxSize() << std::endl;
        std::cout << "ts.length = " << ts.Length() << std::endl;
        std::cout << "ts.Capacity = " << ts.Capacity() << std::endl;
        std::cout << "ts.Compare(ts2) = " << ts.Compare(ts2.Cstr()) << std::endl;
    }

    {
        std::wcout.imbue(std::locale(""));
        TGD::TGDWString wts;
        wts.Append(L"안녕하세요.");
        wts.Append(L"TGDSting은 String을 흉내내서 만든 클래스입니다.");
        wts.Append(L"<test> ");
        wts.Append(L"1234567890 - 0987654321");
        
        TGD::TGDWString wts2(wts);

        std::wcout << L"wts = " << wts.Cstr() << std::endl;
        std::wcout << L"wts2 = " << wts2.Cstr() << std::endl;
        std::wcout << L"wts.maxSize() = " << wts.MaxSize() << std::endl;
        std::wcout << L"wts.length = " << wts.Length() << std::endl;
        std::wcout << L"wts.Capacity = " << wts.Capacity() << std::endl;
        std::wcout << L"wts.Compare(wts2) = " << wts.Compare(wts2.Cstr()) << std::endl;
        std::wcout << L"wts.Compare() = " << wts.Compare(L"안녕하세요.TGDSting은 String을 흉내내서 만든 클래스입니다.") << std::endl;
    }
    return 0;
}