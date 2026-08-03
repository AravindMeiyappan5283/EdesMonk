from PIL import Image
import os

# Open and process the image
img_path = os.path.join(os.getcwd(), 'Edesmonk1.png')
img = Image.open(img_path)
img = img.convert('RGBA')

# Get image data
data = img.getdata()

# Remove white and near-white background
new_data = []
for item in data:
    # If pixel is white or near-white (R > 250, G > 250, B > 250), make transparent
    if item[0] > 250 and item[1] > 250 and item[2] > 250:
        new_data.append((255, 255, 255, 0))
    else:
        new_data.append(item)

# Apply new data
img.putdata(new_data)

# Save
img.save(img_path)
print('White background removed successfully!')
