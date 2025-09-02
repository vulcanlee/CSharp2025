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
}

function App() {
  const [count, setCount] = useState(0)
  const [forecasts, setForecasts] = useState<WeatherForecast[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  // 獲取天氣預報資料的函數
  const fetchWeatherForecast = async () => {
    setLoading(true)
    setError('')
    
    try {
        const response = await fetch('https://localhost:7074/weatherforecast')
      
      if (!response.ok) {
        throw new Error(`API 請求失敗: ${response.status}`)
      }
      
      const data = await response.json()
      setForecasts(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : '獲取天氣預報時發生錯誤')
      console.error('獲取天氣預報時發生錯誤:', err)
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
      
      {/* 天氣預報區塊 */}
      <div className="card" style={{ marginTop: '20px' }}>
        <h2>天氣預報</h2>
        <button onClick={fetchWeatherForecast} disabled={loading}>
          {loading ? '獲取中...' : '獲取天氣預報'}
        </button>
        
        {error && <p style={{ color: 'red' }}>{error}</p>}
        
        {forecasts.length > 0 && (
          <div style={{ marginTop: '20px' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr>
                  <th style={{ border: '1px solid #ddd', padding: '8px' }}>日期</th>
                  <th style={{ border: '1px solid #ddd', padding: '8px' }}>溫度 (C)</th>
                  <th style={{ border: '1px solid #ddd', padding: '8px' }}>溫度 (F)</th>
                  <th style={{ border: '1px solid #ddd', padding: '8px' }}>概況</th>
                </tr>
              </thead>
              <tbody>
                {forecasts.map((forecast, index) => (
                  <tr key={index}>
                    <td style={{ border: '1px solid #ddd', padding: '8px' }}>{forecast.date}</td>
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