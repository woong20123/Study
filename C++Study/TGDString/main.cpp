#include "TGDString.h"
#include <iostream>

int main(int argc, char *argv[])
{
    char a = char();
    std::cout << "a = " << a << std::endl;

    TGD::TGDString ts;
    std::cout << "max size = " << ts.MaxSize() << std::endl;
    return 0;
}