#pragma once

#ifdef TGDSTRINGDLL_EXPORTS
#define DllAPI __declspec(dllexport)
#else
#define DllAPI __declspec(dllimport)
#endif

namespace TGD
{
    /// @brief TGDString의 구현부 클래스.
	class DllAPI TGDString
    {
    public:
        using Element = char;

        TGDString() : ptr(nullptr), size(0), reserved(0)
        {
            DefaultAllocate();
        }
        virtual ~TGDString()
        {
        }

        TGDString(const Element* chars);
        TGDString(const TGDString& string);
        TGDString(TGDString&& string) noexcept;

        /// @brief TGDString에 문자열을 추가합니다. 
        /// @param elems 추가할 문자열 정보
        void Append(const Element* elems);

        /// @brief TGDString에 다른 TGDString 문자열을 추가합니다. 
        /// @param string 추가할 다른 TGDString
        void Append(const TGDString& string);

        /// @brief TGDSting의 내부 문자열 전용 메모리를 예약하는 함수
        /// @param reserved 예약할 메모리 크기
        void Reserve(const size_t reserved);

        /// @brief TGDSting의 문자열 길이를 리턴하는 함수
        /// @return TGDSting의 문자열 길이
        const size_t Length() const noexcept;

        /// @brief TGDString의 내부 문자열을 char * 형태로 리턴하는 함수
        /// @return TGDString의 내부 문자열 주소 
        const Element* Cstr() const noexcept;

        /// @brief TGDString이 비어 있는지 확인하는 함수
        /// @return String의 문자열이 비어 있다면 true리턴
        const bool Empty() const noexcept;

        /// @brief 예약된 메모리 사이즈를 리턴하는 함수
        /// @return 예약된 메모리 사이즈
        const size_t Capacity() const noexcept;

        /// @brief 입력된 문자열과 String을 비교합니다. 
        /// @param elems String과 비교할 문자열
        /// @return 문자가 일치하면 0, 다를경우 -1, 1 
        const int Compare(const Element* elems) const noexcept;

        /// @brief TGD String의 최대 사이즈를 리턴합니다.
        /// @return TGD String의 최대 사이즈
        constexpr static size_t MaxSize() noexcept
        {    
            return static_cast<unsigned long long>(-1);
        }

        constexpr size_t ElementSize()
        {
            return sizeof(Element);
        }

    private:
        /// @brief 인터페이스 Append함수의 실제 구현 함수.
        /// @param chars 추가할 문자열 정보
        /// @param appendSize 추가할 문자열 길이
        void Append(const Element* elems, const size_t appendSize);

        /// @brief 새로운 메모리를 할당하는 함수
        /// @param allocSize 할당할 메모리 크기
        /// @param stringSize TGD에 저장된 문자열의 크기
        void Allocate(const size_t allocSize, const size_t stringSize);

        /// @brief 메모리 재할당이 필요한지 체크한 후에 필요한 경우 메모리를 재할당합니다. 
        /// @param needMemSize 필요한 메모리의 크기
        void ReallocateGrowby(const size_t needMemSize);

        /// @brief 재할당시 필요한 메모리 사이즈를 구하는 함수
        /// @param newSize 최소 새로 할당 해야할 메모리 사이즈 
        /// @return 실제로 할당할 메모리 사이즈 
        const size_t GetGrowth(const size_t newSize) const;

        /// @brief TGDString가 생성시 기본적으로 메모리를 할당하는데 사용하는 함수
        /// 기본 할당 메모리를 사용하면 재할당 횟수를 줄여서 최적화에 도움이 됩니다. 
        void DefaultAllocate();

        /// @brief 입력된 문자열과 TGDString을 비교합니다. 
        /// @param elems 입력된 Element's 
        /// @param compareSize 비교할 문자 길이
        /// @return 같으면 0, 다르면 -1, 1
        int Compare(const Element* elems, const size_t compareSize) const noexcept;

        /// @brief 입력된 Element's 의 크기를 구하는 함수 
        /// @param elems 입력된 Element's 
        /// @return Element's 크기를 리턴
        size_t Strlen(const Element* elems) const noexcept;

        void Memcpy(Element* dest, Element* source, const size_t copySize);
        void MemZeroSet(Element* dest, const size_t setSize);

        // 기본 메모리 할당 사이트
        static constexpr size_t DEFAULT_ALLOCATE_SIZE = 15;

        /// @brief 문자열 저장 주소
        Element* ptr;
        /// @brief 문자열 길이 정보
        size_t size;
        /// @brief 예약된 메모리 길이 정보 
        size_t reserved;
    };
}

