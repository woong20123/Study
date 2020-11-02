// dllmain.cpp : DLL 애플리케이션의 진입점을 정의합니다.
#include "pch.h"

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        //std::cout << "process attach" << std::endl;
    case DLL_THREAD_ATTACH:
        //std::cout << "thread attach" << std::endl;
    case DLL_THREAD_DETACH:
        //std::cout << "thread detach" << std::endl;
    case DLL_PROCESS_DETACH:
        //std::cout << "process detach" << std::endl;
        break;
    }
    return TRUE;
}

