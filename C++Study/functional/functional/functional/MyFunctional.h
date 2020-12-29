#pragma once

template<typename RT, typename... ATs>
class CLambdaInterface
{
public: 
	RT Call(ATs... args) {
		return _Call(args...);
	}

	virtual RT _Call(ATs... args) = 0;
};

template<typename LAMBDA, typename RT, typename... ATs>
class CLambda : public CLambdaInterface<RT, ATs...>
{
public:
	CLambda(const LAMBDA& lambda)
	{
		m_pLambda = new LAMBDA(lambda);
	}
	~CLambda()
	{
		if (m_pLambda) {
			delete m_pLambda;
			m_pLambda = nullptr;
		}
	}

	virtual RT _Call(ATs... args) 
	{
		return (*m_pLambda)(args...);
	}
private:
	LAMBDA* m_pLambda;
};

template<typename T>
class CFunction;

template<typename RT, typename... ATs>
class CFunction<RT(ATs...)>
{
public:
	CFunction() {
		m_pLambdaInterface = nullptr;
	}

	template<typename LAMBDA>
	CFunction(const LAMBDA& lambda) 
	{
		m_pLambdaInterface = new CLambda<LAMBDA, RT, ATs...>(lambda);
	}

	template<typename LAMBDA>
	CFunction<RT(ATs...)>& operator=(const LAMBDA& lambda) 
	{
		if (m_pLambdaInterface)
		{
			delete m_pLambdaInterface;
		}

		m_pLambdaInterface = new CLambda<LAMBDA, RT, ATs...>(lambda);
		return *this;
	}

	RT operator()(ATs... args) {
		return m_pLambdaInterface->Call(args...);
	}
private:
	CLambdaInterface<RT, ATs...>* m_pLambdaInterface;
};