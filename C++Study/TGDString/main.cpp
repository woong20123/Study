#include "TGDString.h"
#include <iostream>

int main(int argc, char *argv[])
{
    char a = char();
    std::cout << "a = " << a << std::endl;

    TGD::TGDString ts;
    std::cout << "maxsize = " << ts.MaxSize() << std::endl;

    std::string b;
    b.length();
    b.append("1");
    b.append("12");
    b.append("123");
    b.append("123456");
    b.append("12345678999999999999999999999999999999999999999999999999999999999999999");
    return 0;
}