import { useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'

// 定義 WeatherForecast 型別，對應後端的資料結構
interface WeatherForecast {
    date: string;
    temperatureC: number;
    temperatureF: number;
    summary: string;
    location?: string;
}

// 定義 ApiResult 型別，對應後端的 ApiResult 包裝格式
interface ApiResult<T> {
    isSuccess: boolean;
    message: string;
    data: T;
    timestamp: string;
}

// 定義 POST 請求的資料格式
interface WeatherRequest {
    location: string;
    requestTime?: string;
}

function App() {
    const [count, setCount] = useState(0)
    const [forecasts, setForecasts] = useState<WeatherForecast[]>([])
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState('')
    const [location, setLocation] = useState('台北')
    const [requestTime, setRequestTime] = useState('')
    const [apiMessage, setApiMessage] = useState('')

    // 獲取天氣預報資料的函數 (使用 POST 方法)
    const fetchWeatherForecast = async () => {
        if (!location.trim()) {
            setError('請輸入地點')
            return
        }

        setLoading(true)
        setError('')
        setApiMessage('')

        try {
            // 準備 POST 請求的資料
            const requestData: WeatherRequest = {
                location: location.trim(),
                requestTime: requestTime || undefined
            }

            const response = await fetch('https://localhost:7074/weatherforecast', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(requestData)
            })

            if (!response.ok) {
                throw new Error(`API 請求失敗: ${response.status}`)
            }

            // 解析 ApiResult 格式的回應
            const apiResult: ApiResult<WeatherForecast[]> = await response.json()
            
            if (apiResult.isSuccess) {
                setForecasts(apiResult.data || [])
                setApiMessage(apiResult.message)
            } else {
                setError(apiResult.message)
            }
        } catch (err) {
            setError(err instanceof Error ? err.message : '獲取天氣預報時發生錯誤')
            console.error('獲取天氣預報時發生錯誤:', err)
        } finally {
            setLoading(false)
        }
    }

    // 格式化日期時間字串供輸入使用
    const getCurrentDateTime = () => {
        const now = new Date()
        return now.toISOString().slice(0, 16) // YYYY-MM-DDTHH:mm 格式
    }

    return (
        <>
            <div>
                <a href="https://vite.dev" target="_blank">
                    <img src={viteLogo} className="logo" alt="Vite logo" />
                </a>
                <a href="https://react.dev" target="_blank">
                    <img src={reactLogo} className="logo react" alt="React logo" />
                </a>
            </div>
            <h1>Vite + React</h1>
            <div className="card">
                <button onClick={() => setCount((count) => count + 1)}>
                    count is {count}
                </button>
                <p>
                    Edit <code>src/App.tsx</code> and save to test HMR
                </p>
            </div>

            {/* 天氣預報區塊 */}
            <div className="card" style={{ marginTop: '20px' }}>
                <h2>天氣預報</h2>
                
                {/* 輸入表單 */}
                <div style={{ marginBottom: '20px', textAlign: 'left' }}>
                    <div style={{ marginBottom: '10px' }}>
                        <label style={{ display: 'block', marginBottom: '5px' }}>
                            地點 (必填):
                        </label>
                        <input
                            type="text"
                            value={location}
                            onChange={(e) => setLocation(e.target.value)}
                            placeholder="請輸入地點，例如：台北"
                            style={{ 
                                padding: '8px', 
                                width: '200px',
                                border: '1px solid #ddd',
                                borderRadius: '4px'
                            }}
                        />
                    </div>
                    
                    <div style={{ marginBottom: '10px' }}>
                        <label style={{ display: 'block', marginBottom: '5px' }}>
                            請求時間 (選填):
                        </label>
                        <input
                            type="datetime-local"
                            value={requestTime}
                            onChange={(e) => setRequestTime(e.target.value)}
                            placeholder={getCurrentDateTime()}
                            style={{ 
                                padding: '8px', 
                                width: '200px',
                                border: '1px solid #ddd',
                                borderRadius: '4px'
                            }}
                        />
                        <small style={{ display: 'block', color: '#666', marginTop: '5px' }}>
                            不填寫則使用當前時間
                        </small>
                    </div>
                </div>

                <button onClick={fetchWeatherForecast} disabled={loading}>
                    {loading ? '獲取中...' : '獲取天氣預報'}
                </button>

                {error && <p style={{ color: 'red', marginTop: '10px' }}>{error}</p>}
                {apiMessage && <p style={{ color: 'green', marginTop: '10px' }}>{apiMessage}</p>}

                {forecasts.length > 0 && (
                    <div style={{ marginTop: '20px' }}>
                        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                            <thead>
                                <tr>
                                    <th style={{ border: '1px solid #ddd', padding: '8px' }}>日期</th>
                                    <th style={{ border: '1px solid #ddd', padding: '8px' }}>地點</th>
                                    <th style={{ border: '1px solid #ddd', padding: '8px' }}>溫度 (C)</th>
                                    <th style={{ border: '1px solid #ddd', padding: '8px' }}>溫度 (F)</th>
                                    <th style={{ border: '1px solid #ddd', padding: '8px' }}>概況</th>
                                </tr>
                            </thead>
                            <tbody>
                                {forecasts.map((forecast, index) => (
                                    <tr key={index}>
                                        <td style={{ border: '1px solid #ddd', padding: '8px' }}>{forecast.date}</td>
                                        <td style={{ border: '1px solid #ddd', padding: '8px' }}>{forecast.location || '未指定'}</td>
                                        <td style={{ border: '1px solid #ddd', padding: '8px' }}>{forecast.temperatureC}</td>
                                        <td style={{ border: '1px solid #ddd', padding: '8px' }}>{forecast.temperatureF}</td>
                                        <td style={{ border: '1px solid #ddd', padding: '8px' }}>{forecast.summary}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            <p className="read-the-docs">
                Click on the Vite and React logos to learn more
            </p>
        </>
    )
}

export default App