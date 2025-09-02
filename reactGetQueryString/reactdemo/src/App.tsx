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
    location: string;
}

function App() {
    const [count, setCount] = useState(0)
    const [forecasts, setForecasts] = useState<WeatherForecast[]>([])
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState('')

    // 使用者輸入地點與日期
    const [location, setLocation] = useState('Kaohsiung')
    const [date, setDate] = useState(() => new Date().toISOString().split('T')[0]) // YYYY-MM-DD

    const apiBase = 'https://localhost:7074'

    // 呼叫後端 GetWeatherByLocationAndDate API
    const fetchWeatherByLocationAndDate = async () => {
        setLoading(true)
        setError('')
        setForecasts([])

        try {
            if (!location.trim()) {
                throw new Error('地點不可為空')
            }
            if (!/^\d{4}-\d{2}-\d{2}$/.test(date)) {
                throw new Error('日期格式需為 YYYY-MM-DD')
            }

            const query = new URLSearchParams({
                location: location.trim(),
                date: date
            }).toString()

            const response = await fetch(`${apiBase}/weatherforecast/GetWeatherByLocationAndDate?${query}`)

            if (!response.ok) {
                throw new Error(`API 請求失敗: ${response.status}`)
            }

            const data = await response.json() as WeatherForecast[]
            setForecasts(data)
        } catch (err) {
            setError(err instanceof Error ? err.message : '取得資料時發生錯誤')
            console.error('GetWeatherByLocationAndDate 呼叫失敗:', err)
        } finally {
            setLoading(false)
        }
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

            {/* 自訂查詢天氣預報 */}
            <div className="card" style={{ marginTop: '20px', width: '100%', maxWidth: 800 }}>
                <h2>查詢指定地點與日期的天氣預報</h2>

                <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap', marginBottom: '12px' }}>
                    <div style={{ display: 'flex', flexDirection: 'column' }}>
                        <label htmlFor="location">地點</label>
                        <input
                            id="location"
                            type="text"
                            value={location}
                            onChange={e => setLocation(e.target.value)}
                            placeholder="輸入地點"
                            style={{ padding: '6px 8px' }}
                        />
                    </div>
                    <div style={{ display: 'flex', flexDirection: 'column' }}>
                        <label htmlFor="date">日期 (YYYY-MM-DD)</label>
                        <input
                            id="date"
                            type="date"
                            value={date}
                            onChange={e => setDate(e.target.value)}
                            style={{ padding: '6px 8px' }}
                        />
                    </div>
                    <div style={{ alignSelf: 'flex-end' }}>
                        <button
                            onClick={fetchWeatherByLocationAndDate}
                            disabled={loading || !location.trim() || !/^\d{4}-\d{2}-\d{2}$/.test(date)}
                            style={{ padding: '8px 14px' }}
                        >
                            {loading ? '查詢中...' : '取得天氣'}
                        </button>
                    </div>
                </div>

                {error && <p style={{ color: 'red' }}>{error}</p>}

                {forecasts.length > 0 && (
                    <div style={{ marginTop: '12px', overflowX: 'auto' }}>
                        <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: 600 }}>
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
                                        <td style={{ border: '1px solid #ddd', padding: '8px' }}>{forecast.location}</td>
                                        <td style={{ border: '1px solid #ddd', padding: '8px' }}>{forecast.temperatureC}</td>
                                        <td style={{ border: '1px solid #ddd', padding: '8px' }}>{forecast.temperatureF}</td>
                                        <td style={{ border: '1px solid #ddd', padding: '8px' }}>{forecast.summary}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}

                {forecasts.length === 0 && !loading && !error && (
                    <p style={{ marginTop: '8px', color: '#666' }}>請輸入地點與日期後點擊「取得天氣」。</p>
                )}
            </div>

            <p className="read-the-docs">
                Click on the Vite and React logos to learn more
            </p>
        </>
    )
}

export default App