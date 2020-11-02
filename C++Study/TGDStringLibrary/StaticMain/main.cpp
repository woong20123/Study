#include <iostream>
#include "../TGDStringStatic/TGDString.h"

int main(int argc, char* argv[])
{
	TGD::TGDString s;

	std::cout << "[Load static Library]" << std::endl;
	s.Append("1111");
	s.Append("2222");
	s.Append("3333");
	s.Append("4444");
	s.Append("5555");
	s.Append("6666");
	s.Append("7777");
	std::cout << s.Cstr() << std::endl;
	getchar();
}

