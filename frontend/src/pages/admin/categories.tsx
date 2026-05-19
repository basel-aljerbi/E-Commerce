import { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { Plus, Edit, Trash2, Image as ImageIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Card, CardContent, CardHeader, CardTitle,
} from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Dialog } from '@/components/ui/dialog';
import { useAdminCategories, useAdminCreateCategory, useAdminUpdateCategory, useAdminDeleteCategory } from '@/hooks/useAdmin';
import { getImageUrl } from '@/lib/utils';

export default function AdminCategoriesPage() {
  const [modalOpen, setModalOpen] = useState(false);
  const [editId, setEditId] = useState<number | null>(null);
  const [name, setName] = useState('');
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<{ id: number; name: string } | null>(null);

  const { data, isLoading } = useAdminCategories();
  const createCategory = useAdminCreateCategory();
  const updateCategory = useAdminUpdateCategory();
  const deleteCategory = useAdminDeleteCategory();

  const categories = data?.data ?? [];

  useEffect(() => {
    if (!modalOpen) { setEditId(null); setName(''); setImageFile(null); setPreviewUrl(null); }
  }, [modalOpen]);

  const openCreate = () => {
    setEditId(null); setName(''); setImageFile(null); setPreviewUrl(null);
    setModalOpen(true);
  };

  const openEdit = (cat: { id: number; name: string; imageUrl: string | null }) => {
    setEditId(cat.id); setName(cat.name); setPreviewUrl(cat.imageUrl); setImageFile(null);
    setModalOpen(true);
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    setImageFile(file);
    if (file) {
      const reader = new FileReader();
      reader.onload = () => setPreviewUrl(reader.result as string);
      reader.readAsDataURL(file);
    } else {
      // If editing and file cleared, don't clear preview until save
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;
    if (editId !== null) {
      await updateCategory.mutateAsync({ id: editId, name: name.trim(), image: imageFile ?? undefined });
    } else {
      await createCategory.mutateAsync({ name: name.trim(), image: imageFile ?? undefined });
    }
    setModalOpen(false);
  };

  const handleDelete = async (id: number) => {
    await deleteCategory.mutateAsync(id);
    setDeleteConfirm(null);
  };

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }}>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-3xl font-bold">Categories</h1>
        <Button onClick={openCreate}>
          <Plus className="h-4 w-4 mr-2" /> Add Category
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All Categories</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 3 }).map((_, i) => (
                <Skeleton key={i} className="h-16 rounded-lg" />
              ))}
            </div>
          ) : categories.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="border-b text-left text-sm text-muted-foreground">
                    <th className="pb-3 font-medium">Image</th>
                    <th className="pb-3 font-medium">ID</th>
                    <th className="pb-3 font-medium">Name</th>
                    <th className="pb-3 font-medium">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {categories.map((cat: any) => (
                    <tr key={cat.id} className="border-b last:border-0 hover:bg-muted/50 transition-colors">
                      <td className="py-3">
                        <div className="h-10 w-10 shrink-0 rounded-md bg-muted overflow-hidden">
                          {cat.imageUrl ? (
                            <img src={getImageUrl(cat.imageUrl)} alt={cat.name} className="h-full w-full object-cover" />
                          ) : (
                            <div className="flex h-full items-center justify-center text-muted-foreground">
                              <ImageIcon className="h-5 w-5" />
                            </div>
                          )}
                        </div>
                      </td>
                      <td className="py-3 text-sm text-muted-foreground">{cat.id}</td>
                      <td className="py-3 font-medium">{cat.name}</td>
                      <td className="py-3">
                        <div className="flex items-center gap-1">
                          <Button variant="ghost" size="icon" onClick={() => openEdit(cat)}>
                            <Edit className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost" size="icon" className="text-destructive"
                            onClick={() => setDeleteConfirm(cat)}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground py-8 text-center">No categories yet</p>
          )}
        </CardContent>
      </Card>

      <Dialog open={modalOpen} onOpenChange={setModalOpen} title={editId !== null ? 'Edit Category' : 'Add Category'}>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="catName">Category Name</Label>
            <Input id="catName" value={name} onChange={(e) => setName(e.target.value)} placeholder="Enter category name" required />
          </div>
          <div className="space-y-2">
            <Label htmlFor="catImage">Image</Label>
            <Input id="catImage" type="file" accept="image/*" onChange={handleFileChange} />
            {previewUrl && (
              <div className="mt-2 h-32 w-32 overflow-hidden rounded-md bg-muted">
                <img src={previewUrl.startsWith('data:') ? previewUrl : getImageUrl(previewUrl)} alt="Preview" className="h-full w-full object-cover" />
              </div>
            )}
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setModalOpen(false)}>Cancel</Button>
            <Button type="submit" disabled={createCategory.isPending || updateCategory.isPending}>
              {editId !== null ? 'Update' : 'Create'}
            </Button>
          </div>
        </form>
      </Dialog>

      <Dialog open={deleteConfirm !== null} onOpenChange={() => setDeleteConfirm(null)} title="Delete Category">
        <p className="text-sm text-muted-foreground mb-2">
          Are you sure you want to delete <strong>{deleteConfirm?.name}</strong>?
        </p>
        <p className="text-sm text-destructive mb-4">This will fail if the category has products.</p>
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={() => setDeleteConfirm(null)}>Cancel</Button>
          <Button variant="destructive" onClick={() => deleteConfirm && handleDelete(deleteConfirm.id)}>
            Delete
          </Button>
        </div>
      </Dialog>
    </motion.div>
  );
}
