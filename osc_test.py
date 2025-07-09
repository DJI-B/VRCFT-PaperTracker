#!/usr/bin/env python3
"""
OSC测试脚本 - 用于测试PaperTracker模块的OSC接收功能
需要安装: pip install python-osc
"""

import time
import argparse
from pythonosc import udp_client

def test_face_tracking_osc():
    """测试面部追踪OSC消息"""
    
    # 创建OSC客户端
    client = udp_client.SimpleUDPClient("127.0.0.1", 8888)
    
    print("开始发送面部追踪OSC消息到 127.0.0.1:8888...")
    
    # 测试消息列表
    test_messages = [
        ("/jawOpen", 0.5),
        ("/mouthSmileLeft", 0.3),
        ("/mouthSmileRight", 0.3),
        ("/mouthFrownLeft", 0.1),
        ("/mouthFrownRight", 0.1),
        ("/cheekPuffLeft", 0.2),
        ("/cheekPuffRight", 0.2),
        ("/tongueOut", 0.4),
        ("/mouthFunnel", 0.3),
        ("/mouthPucker", 0.2),
    ]
    
    for i, (address, value) in enumerate(test_messages):
        print(f"发送消息 {i+1}/10: {address} = {value}")
        try:
            client.send_message(address, value)
            time.sleep(0.5)  # 等待0.5秒
        except Exception as e:
            print(f"发送消息失败: {e}")
    
    print("测试完成!")

def test_eye_tracking_osc():
    """测试眼部追踪OSC消息"""
    
    # 创建OSC客户端
    client = udp_client.SimpleUDPClient("127.0.0.1", 8889)
    
    print("开始发送眼部追踪OSC消息到 127.0.0.1:8889...")
    
    # 测试消息列表
    test_messages = [
        ("/RightEyeLidExpandedSqueeze", 0.8),
        ("/LeftEyeLidExpandedSqueeze", 0.8),
        ("/RightEyeX", 0.1),
        ("/LeftEyeX", -0.1),
        ("/EyesY", 0.2),
        ("/EyesDilation", 0.6),
    ]
    
    for i, (address, value) in enumerate(test_messages):
        print(f"发送消息 {i+1}/6: {address} = {value}")
        try:
            client.send_message(address, value)
            time.sleep(0.5)  # 等待0.5秒
        except Exception as e:
            print(f"发送消息失败: {e}")
    
    print("测试完成!")

def continuous_test():
    """持续测试模式"""
    
    client = udp_client.SimpleUDPClient("127.0.0.1", 8888)
    
    print("持续发送测试消息 (按 Ctrl+C 停止)...")
    
    try:
        counter = 0
        while True:
            # 发送一个简单的测试消息
            value = (counter % 100) / 100.0  # 0.0 到 0.99 循环
            client.send_message("/jawOpen", value)
            print(f"发送: /jawOpen = {value:.2f}")
            
            counter += 1
            time.sleep(1)
            
    except KeyboardInterrupt:
        print("\n测试停止")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='OSC测试工具')
    parser.add_argument('--mode', choices=['face', 'eye', 'continuous'], 
                       default='face', help='测试模式')
    
    args = parser.parse_args()
    
    if args.mode == 'face':
        test_face_tracking_osc()
    elif args.mode == 'eye':
        test_eye_tracking_osc()
    elif args.mode == 'continuous':
        continuous_test()