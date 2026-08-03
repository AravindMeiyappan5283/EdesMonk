from PIL import Image
import numpy as np

img = Image.open('Edesmonk3.png')

if img.mode != 'RGBA':
    img = img.convert('RGBA')

data = np.array(img)

r = data[:,:,0]
g = data[:,:,1]
b = data[:,:,2]
a = data[:,:,3]

# Target bright lime green (#7ac74f and similar)
# Bright green has high G value, moderate R, lower B
green_mask = (g > 140) & (r < 180) & (b < 100)

data[green_mask, 3] = 0

result = Image.fromarray(data)
result.save('Edesmonk3.png')

print("Green background removed!")
