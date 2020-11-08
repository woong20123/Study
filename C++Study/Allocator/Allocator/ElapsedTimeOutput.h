#pragma once
#include <string>
#if(_MSC_VER >= 1900 )
#include <chrono>
#else
#include <windows.h>
#endif





// Visual Studio 2015 이상에서는 std::chrono를 사용해서 구현합니다.
// Visual Studio 2015 미만에서는 WIN API 함수인 QueryPerformanceCounter함수를 사용해서 구현합니다.
#if(_MSC_VER >= 1900 )
/**
	@class : ElapsedTimeOutput
	@date : 2019-09-20
	@brief : 경과시간을 화면에 프린트합니다.
*/
class ElapsedTimeOutput
{
public:


	ElapsedTimeOutput() : m_start(Now())
	{}
	ElapsedTimeOutput(const std::string& name) : m_start(Now()), m_name(name)
	{}
	virtual ~ElapsedTimeOutput()
	{}

	ElapsedTimeOutput(const ElapsedTimeOutput& rhs) = delete;
	ElapsedTimeOutput& operator=(const ElapsedTimeOutput& rhs) = delete;

	using sc_clock = std::chrono::system_clock;



	/// @brief : Sec 단위 시간을 출력합니다.
	void SecPrint()
	{
		GetElapsedTime<std::chrono::seconds>("seconds");
	}

	/// @brief : milliSec 단위 시간을 출력합니다.
	void milliSecPrint()
	{
		GetElapsedTime<std::chrono::milliseconds>("millie seconds");
	}

	/// @brief : microSec 단위 시간을 출력합니다.
	void microSecPrint()
	{
		GetElapsedTime<std::chrono::microseconds>("micro seconds");
	}

	/// @brief : nanoSec 단위 시간을 출력합니다.
	void nanoSecPrint()
	{
		GetElapsedTime<std::chrono::nanoseconds>("nano seconds");
	}

	/// @brief : 시작시간을 초기화 합니다.
	void SetStartTime()
	{
		m_start = Now();
	}

protected:
	template<typename T>
	void GetElapsedTime(std::string timeType)
	{
		T value = std::chrono::duration_cast<T>(Now() - m_start);
		if (m_name != "")
			std::cout << "[" << m_name.c_str() << "]" << std::endl;
		std::cout << "ElapsedTime : " << value.count() << " " << timeType.c_str() << std::endl;
	}

	std::chrono::time_point<sc_clock> Now()
	{
		return sc_clock::now();
	}
private:
	sc_clock::time_point m_start;
	std::string m_name;
};
#else
/**
	@class : ElapsedTimeOutput
	@date : 2019-09-20
	@brief : 경과시간을 화면에 프린트합니다.
*/
class ElapsedTimeOutput
{
public:

	typedef __int64 TimeCheck;

	ElapsedTimeOutput() : m_start(Now()), m_freq(GetFreq())
	{}
	ElapsedTimeOutput(const std::string& name) : m_start(Now()), m_freq(GetFreq()), m_name(name)
	{}
	virtual ~ElapsedTimeOutput()
	{}

	/// @brief : Sec 단위 시간을 출력합니다.
	void SecPrint()
	{
		GetElapsedTime("seconds");
	}

	/// @brief : milliSec 단위 시간을 출력합니다.
	void milliSecPrint()
	{
		GetElapsedTime("millie seconds", 1000);
	}

	/// @brief : microSec 단위 시간을 출력합니다.
	void microSecPrint()
	{
		GetElapsedTime("micro seconds", 1000000);
	}

	/// @brief : nanoSec 단위 시간을 출력합니다.
	void nanoSecPrint()
	{
		GetElapsedTime("nano seconds", 1000000000);
	}

	/// @brief : 시작시간을 초기화 합니다.
	void SetStartTime()
	{
		m_start = Now();
	}

protected:
	bool GetElapsedTime(std::string timeType, int N = 0)
	{
		TimeCheck freq = m_freq;

		if (N > freq) {
			std::cout << "Not Supported " << timeType.c_str() << std::endl;
			return false;
		}

		if (N > 1)
		{
			freq /= N;
		}
		TimeCheck value = (Now() - m_start) / freq;
		if (m_name != "")
			std::cout << "[" << m_name.c_str() << "]" << std::endl;
		std::cout << "ElapsedTime : " << value << " " << timeType.c_str() << std::endl;
		return true;
	}

	TimeCheck Now()
	{
		LARGE_INTEGER qpcTime;
		QueryPerformanceCounter(&qpcTime);
		return qpcTime.QuadPart;
	}

	TimeCheck GetFreq()
	{
		LARGE_INTEGER qpcRate;
		QueryPerformanceFrequency(&qpcRate);
		return qpcRate.QuadPart;
	}

private:
	TimeCheck m_start;
	TimeCheck m_freq;
	std::string m_name;

	ElapsedTimeOutput(const ElapsedTimeOutput& rhs);
	ElapsedTimeOutput& operator=(const ElapsedTimeOutput& rhs);
};
#endif