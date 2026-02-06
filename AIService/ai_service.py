from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Optional, List
from fastapi.middleware.cors import CORSMiddleware
import requests
import json
from datetime import datetime
import os
from dotenv import load_dotenv
import uuid

app = FastAPI(title="Chat App AI Service", description="Optimized for C#/.NET frontend")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"], 
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

class ChatRequest(BaseModel):
    message: str
    conversationId: Optional[str] = None  
    userId: Optional[str] = None
    context: Optional[List[dict]] = None

class ChatResponse(BaseModel):
    reply: str
    conversationId: Optional[str] = None
    messageId: Optional[str] = None
    timestamp: Optional[str] = None
    modelUsed: Optional[str] = None
    success: bool = True
    error: Optional[str] = None

class HealthResponse(BaseModel):
    status: str
    service: str
    timestamp: str
    modelsAvailable: List[str]


load_dotenv()

API_KEY = os.getenv("API_KEY")
GROQ_URL = "https://api.groq.com/openai/v1/chat/completions"


PRIMARY_MODEL = "llama-3.1-8b-instant"
FALLBACK_MODELS = ["mixtral-8x7b-32768", "gemma2-9b-it"]


conversations = {}


def get_current_time():
    return datetime.now().isoformat()

def generate_id():
    return str(uuid.uuid4())

def call_groq_api(messages: List[dict], model: str) -> Optional[dict]:
    """Call Groq API with error handling"""
    headers = {
        "Authorization": f"Bearer {API_KEY}",
        "Content-Type": "application/json"
    }
    
    payload = {
        "model": model,
        "messages": messages,
        "temperature": 0.7,
        "max_tokens": 500,
        "stream": False
    }
    
    try:
        response = requests.post(GROQ_URL, headers=headers, json=payload, timeout=15)
        if response.status_code == 200:
            return response.json()
    except:
        pass
    return None

@app.post("/chat", response_model=ChatResponse)
async def chat(request: ChatRequest):
    """
    Main chat endpoint optimized for C#/.NET
    Returns camelCase JSON for C# deserialization
    """
    try:
        print(f"Received from C#: '{request.message}'")
        
        
        conversation_id = request.conversationId or generate_id()
        message_id = generate_id()
        
        
        messages = [
            {
                "role": "system",
                "content": "You are a helpful AI assistant in a chat application. Be friendly, concise, and helpful."
            }
        ]
        
        
        if request.context:
            for msg in request.context[-10:]:  
                messages.append({
                    "role": msg.get("role", "user"),
                    "content": msg.get("content", "")
                })
        
        
        messages.append({
            "role": "user",
            "content": request.message
        })
        
       
        ai_response = None
        model_used = None
        
        
        result = call_groq_api(messages, PRIMARY_MODEL)
        if result:
            ai_response = result["choices"][0]["message"]["content"]
            model_used = PRIMARY_MODEL
        else:
            
            for model in FALLBACK_MODELS:
                result = call_groq_api(messages, model)
                if result:
                    ai_response = result["choices"][0]["message"]["content"]
                    model_used = model
                    break
        
        if not ai_response:
            return ChatResponse(
                reply="I'm having trouble connecting to the AI service. Please try again.",
                conversationId=conversation_id,
                messageId=message_id,
                timestamp=get_current_time(),
                modelUsed="error",
                success=False,
                error="All AI models failed"
            )
        
        print(f"Response ready: {ai_response[:50]}...")
        
        return ChatResponse(
            reply=ai_response,
            conversationId=conversation_id,
            messageId=message_id,
            timestamp=get_current_time(),
            modelUsed=model_used,
            success=True
        )
        
    except Exception as e:
        print(f" Error: {str(e)}")
        return ChatResponse(
            reply="An unexpected error occurred.",
            success=False,
            error=str(e)[:100]
        )

@app.get("/health")
async def health_check():
    """Health check for C# client"""
    return HealthResponse(
        status="healthy",
        service="Chat AI Service",
        timestamp=get_current_time(),
        modelsAvailable=[PRIMARY_MODEL] + FALLBACK_MODELS
    )

@app.get("/")
async def root():
    """Simple root endpoint"""
    return {
        "service": "Chat AI Backend",
        "version": "1.0.0",
        "endpoints": {
            "POST /chat": "Send chat message",
            "GET /health": "Health check",
            "GET /": "This info"
        }
    }

@app.post("/echo")
async def echo_test(request: dict):
    """Echo test endpoint for debugging C# connection"""
    return {
        "received": request,
        "timestamp": get_current_time(),
        "echo": "Test successful"
    }