#include "TGDString.h"
#include <iostream>

int main(int argc, char *argv[])
{
    TGD::TGDString ts;
    ts.Append("12345678901234");
    ts.Append("123");
    ts.Append("456");
    ts.Append("789");
    ts.Append("987654321");
    

    TGD::TGDString ts2(ts);
    ts2.Cstr();


    std::cout << "ts.length = " << ts.Length() << std::endl;
    std::cout << "ts = " << ts.Cstr() << std::endl;
    return 0;
}